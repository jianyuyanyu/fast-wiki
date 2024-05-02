using System.Text;
using System.Text.Json;
using Azure.AI.OpenAI;
using FastWiki.Service.Contracts.OpenAI;
using FastWiki.Service.Domain.Function.Repositories;
using FastWiki.Service.Domain.Storage.Aggregates;
using FastWiki.Service.Infrastructure;
using FastWiki.Service.Infrastructure.Helper;
using Microsoft.KernelMemory.DataFormats.Text;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using TokenApi.Service.Exceptions;

namespace FastWiki.Service.Service;

public class OpenAIService(
    IChatApplicationRepository chatApplicationRepository,
    IFileStorageRepository fileStorageRepository,
    IMapper mapper,
    IFastWikiFunctionCallRepository fastWikiFunctionCallRepository)
{
    public async Task Completions(HttpContext context)
    {
        using var stream = new StreamReader(context.Request.Body);

        var module =
            JsonSerializer.Deserialize<ChatCompletionDto<ChatCompletionRequestMessage>>(await stream.ReadToEndAsync());

        context.Response.ContentType = "text/event-stream";

        if (module == null)
        {
            await context.WriteEndAsync("Invalid request");

            return;
        }

        var logger = context.RequestServices.GetRequiredService<ILogger<OpenAIService>>();

        var chatDialogId = context.Request.Query["ChatDialogId"].ToString();
        var chatId = context.Request.Query["ChatId"];
        var token = context.Request.Headers.Authorization;
        var chatShareId = context.Request.Query["ChatShareId"];


        var eventBus = context.RequestServices.GetRequiredService<IEventBus>();

        ChatShareDto getAPIKeyChatShareQuery = null;

        ChatApplicationDto chatApplication = null;

        if (token.ToString().Replace("Bearer ", "").Trim().StartsWith("sk-"))
        {
            getAPIKeyChatShareQuery = mapper.Map<ChatShareDto>(
                await chatApplicationRepository.GetAPIKeyChatShareAsync(token.ToString().Replace("Bearer ", "")
                    .Trim()));


            if (getAPIKeyChatShareQuery == null)
            {
                context.Response.StatusCode = 401;
                await context.WriteEndAsync("Token无效");
                return;
            }

            chatApplication =
                mapper.Map<ChatApplicationDto>(
                    chatApplicationRepository.FindAsync(getAPIKeyChatShareQuery.ChatApplicationId));
        }
        else
        {
            // 如果不是sk则校验用户 并且不是分享链接
            if (chatShareId.IsNullOrEmpty())
            {
                // 判断当前用户是否登录
                if (context.User.Identity?.IsAuthenticated == false)
                {
                    context.Response.StatusCode = 401;
                    await context.WriteEndAsync("Token不能为空");
                    return;
                }
            }

            // 如果是分享链接则获取分享信息
            if (!chatShareId.IsNullOrEmpty())
            {
                var result = await chatApplicationRepository.GetChatShareAsync(chatShareId);


                // 如果chatShareId不存在则返回让下面扣款
                getAPIKeyChatShareQuery = mapper.Map<ChatShareDto>(result);

                chatApplication =
                    mapper.Map<ChatApplicationDto>(
                        chatApplicationRepository.FindAsync(getAPIKeyChatShareQuery.ChatApplicationId));
            }
            // 如果是应用Id则获取应用信息
            else if (!chatId.IsNullOrEmpty())
            {
                chatApplication = mapper.Map<ChatApplicationDto>(chatApplicationRepository.FindAsync(chatId));
            }

            if (chatApplication == null)
            {
                await context.WriteEndAsync("应用Id不存在");
                return;
            }
        }

        int requestToken = 0;

        var chatHistory = new ChatHistory();

        // 如果设置了Prompt，则添加
        if (!chatApplication.Prompt.IsNullOrEmpty())
        {
            chatHistory.AddSystemMessage(chatApplication.Prompt);
        }

        var content = module.messages.Last();
        var question = content.content;

        var prompt = string.Empty;

        var sourceFile = new List<FileStorage>();
        var wikiMemoryService = context.RequestServices.GetRequiredService<WikiMemoryService>();
        var memoryServerless = wikiMemoryService.CreateMemoryServerless(chatApplication.ChatModel);

        // 如果为空则不使用知识库
        if (chatApplication.WikiIds.Count != 0)
        {
            var filters = chatApplication.WikiIds
                .Select(chatApplication => new MemoryFilter().ByTag("wikiId", chatApplication.ToString())).ToList();

            var result = await memoryServerless.SearchAsync(content.content, "wiki", filters: filters, limit: 3,
                minRelevance: chatApplication.Relevancy);

            var fileIds = new List<long>();

            result.Results.ForEach(x =>
            {
                // 获取fileId
                var fileId = x.Partitions.Select(x => x.Tags.FirstOrDefault(x => x.Key == "fileId"))
                    .FirstOrDefault(x => !x.Value.IsNullOrEmpty())
                    .Value.FirstOrDefault();

                if (!fileId.IsNullOrWhiteSpace() && long.TryParse(fileId, out var id))
                {
                    fileIds.Add(id);
                }

                prompt += string.Join(Environment.NewLine, x.Partitions.Select(x => x.Text));
            });

            if (result.Results.Count == 0 &&
                !string.IsNullOrWhiteSpace(chatApplication.NoReplyFoundTemplate))
            {
                await context.WriteEndAsync(chatApplication.NoReplyFoundTemplate);
                return;
            }

            var tokens = TokenHelper.GetGptEncoding().Encode(prompt);

            // 这里可以有效的防止token数量超出限制，但是也会降低回复的质量
            prompt = TokenHelper.GetGptEncoding()
                .Decode(tokens.Take(chatApplication.MaxResponseToken).ToList());

            // 如果prompt不为空，则需要进行模板替换
            if (!prompt.IsNullOrEmpty())
            {
                prompt = chatApplication.Template.Replace("{{quote}}", prompt)
                    .Replace("{{question}}", content.content);
            }

            // 在这里需要获取源文件
            if (fileIds.Count > 0 && chatApplication.ShowSourceFile)
            {
                sourceFile.AddRange(await fileStorageRepository.GetListAsync(fileIds.ToArray()));
            }

            if (!prompt.IsNullOrEmpty())
            {
                // 删除最后一个消息
                module.messages.RemoveAt(module.messages.Count - 1);
                module.messages.Add(new ChatCompletionRequestMessage()
                {
                    content = prompt,
                    role = "user"
                });
            }
        }

        // 添加用户输入，并且计算请求token数量
        module.messages.ForEach(x =>
        {
            if (!x.content.IsNullOrEmpty())
            {
                requestToken += TokenHelper.ComputeToken(x.content);
                if (x.role == "user")
                {
                    chatHistory.AddUserMessage(x.content);
                }
                else if (x.role == "assistant")
                {
                    chatHistory.AddSystemMessage(x.content);
                }
                else if (x.role == "system")
                {
                    chatHistory.AddSystemMessage(x.content);
                }
            }
        });


        if (getAPIKeyChatShareQuery != null)
        {
            // 如果token不足则返回，使用token和当前request总和大于可用token，则返回
            if (getAPIKeyChatShareQuery.AvailableToken != -1 &&
                (getAPIKeyChatShareQuery.UsedToken + requestToken) >=
                getAPIKeyChatShareQuery.AvailableToken)
            {
                await context.WriteEndAsync("Token不足");
                return;
            }

            // 如果没有过期则继续
            if (getAPIKeyChatShareQuery.Expires != null &&
                getAPIKeyChatShareQuery.Expires < DateTimeOffset.Now)
            {
                await context.WriteEndAsync("Token已过期");
                return;
            }
        }


        var responseId = Guid.NewGuid().ToString("N");
        var requestId = Guid.NewGuid().ToString("N");
        var output = new StringBuilder();
        try
        {
            var functionCall =
                await fastWikiFunctionCallRepository.GetListAsync(x => chatApplication.FunctionIds.Contains(x.Id));

            var kernel =
                wikiMemoryService.CreateFunctionKernel(functionCall?.ToList(), chatApplication.ChatModel);

            // 如果有函数调用
            if (chatApplication.FunctionIds.Any() && functionCall.Any())
            {
                OpenAIPromptExecutionSettings settings = new()
                {
                    ToolCallBehavior = ToolCallBehavior.EnableKernelFunctions
                };

                // TODO: 这里目前还只能支持OpenAI
                var chat = kernel.GetRequiredService<IChatCompletionService>();

                var result =
                    (OpenAIChatMessageContent)await chat.GetChatMessageContentAsync(chatHistory, settings,
                        kernel);

                List<ChatCompletionsFunctionToolCall> toolCalls =
                    result.ToolCalls.OfType<ChatCompletionsFunctionToolCall>().ToList();

                if (toolCalls.Count == 0)
                {
                    await context.WriteEndAsync("未找到函数");
                    return;
                }

                foreach (var toolCall in toolCalls)
                {
                    kernel.Plugins.TryGetFunctionAndArguments(toolCall, out var function,
                        out KernelArguments? arguments);

                    if (function == null)
                    {
                        // await context.WriteEndAsync("未找到函数");
                        // return;
                        logger.LogError("未找到函数");
                        continue;
                    }

                    try
                    {
                        var functionResult = await function.InvokeAsync(kernel, new KernelArguments()
                        {
                            {
                                "value", arguments?.Select(x => x.Value).ToArray()
                            }
                        });
                        // 判断ValueType是否为值类型
                        if (functionResult.ValueType?.IsValueType == true || functionResult.ValueType == typeof(string))
                        {
                            chatHistory.AddAssistantMessage(functionResult.GetValue<object>().ToString());
                        }
                        else
                        {
                            // 记录函数调用
                            chatHistory.AddAssistantMessage(
                                JsonSerializer.Serialize(functionResult.GetValue<object>()));
                        }
                    }
                    catch (Exception e)
                    {
                        await context.WriteEndAsync("函数调用异常：" + e.Message);
                        logger.LogError(e, "函数调用异常");
                        return;
                    }
                }

                await foreach (var item in chat.GetStreamingChatMessageContentsAsync(chatHistory))
                {
                    var message = item.Content;
                    if (string.IsNullOrEmpty(message))
                    {
                        continue;
                    }

                    output.Append(message);
                    await context.WriteOpenAiResultAsync(message, module.model, requestId,
                        responseId);
                }
            }
            else
            {
                var chat = kernel.GetRequiredService<IChatCompletionService>();

                await foreach (var item in chat.GetStreamingChatMessageContentsAsync(chatHistory))
                {
                    var message = item.Content;
                    if (string.IsNullOrEmpty(message))
                    {
                        continue;
                    }

                    output.Append(message);
                    await context.WriteOpenAiResultAsync(message, module.model, requestId,
                        responseId);
                }
            }
        }
        catch (NotModelException notModelException)
        {
            await context.WriteEndAsync("未找到模型兼容：" + notModelException.Message);
            logger.LogError(notModelException, "未找到模型兼容");
            return;
        }
        catch (InvalidOperationException invalidOperationException)
        {
            await context.WriteEndAsync("对话异常：" + invalidOperationException.Message);
            logger.LogError(invalidOperationException, "对话异常");
            return;
        }
        catch (ArgumentException argumentException)
        {
            await context.WriteEndAsync(argumentException.Message);
            logger.LogError(argumentException, "对话异常");
            return;
        }
        catch (Exception e)
        {
            logger.LogError(e, "对话异常");
            await context.WriteEndAsync("对话异常：" + e.Message);
            return;
        }

        await context.WriteEndAsync();

        //对于对话扣款
        if (getAPIKeyChatShareQuery != null)
        {
            await chatApplicationRepository.DeductTokenAsync(getAPIKeyChatShareQuery.Id, requestToken);
        }
    }

    /// <summary>
    /// QA问答解析大文本拆分多个段落
    /// </summary>
    /// <param name="prompt"></param>
    /// <param name="value"></param>
    /// <param name="model"></param>
    /// <param name="apiKey"></param>
    /// <param name="url"></param>
    /// <param name="memoryService"></param>
    /// <returns></returns>
    public static async IAsyncEnumerable<string> QaAsync(string prompt, string value, string model, string apiKey,
        string url,
        WikiMemoryService memoryService)
    {
        var kernel = memoryService.CreateFunctionKernel(apiKey, model, url);

        var qaFunction = kernel.CreateFunctionFromPrompt(prompt, functionName: "QA", description: "QA问答");


        var lines = TextChunker.SplitPlainTextLines(value, 299);
        var paragraphs = TextChunker.SplitPlainTextParagraphs(lines, 4000);

        foreach (var paragraph in paragraphs)
        {
            var result = await kernel.InvokeAsync(qaFunction, new KernelArguments()
            {
                {
                    "input", paragraph
                }
            });

            yield return result.GetValue<string>();
        }
    }

    private static bool IsVision(string model)
    {
        if (model.Contains("vision") || model.Contains("image"))
        {
            return true;
        }

        return false;
    }
}