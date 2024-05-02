using FastWiki.Service.Infrastructure.Helper;

namespace FastWiki.Service.Service;

/// <inheritdoc />
public sealed class ChatApplicationService(
    IChatApplicationRepository chatApplicationRepository,
    IMapper mapper)
    : ApplicationService<ChatApplicationService>
{
    /// <inheritdoc />
    [Authorize]
    public async Task CreateAsync(CreateChatApplicationInput input)
    {
        var chatApplication = new ChatApplication(Guid.NewGuid().ToString("N"))
        {
            Name = input.Name,
        };

        await chatApplicationRepository.AddAsync(chatApplication);
    }

    /// <inheritdoc />
    [Authorize]
    public async Task RemoveAsync(string id)
    {
        await chatApplicationRepository.RemoveAsync(id);
    }

    /// <inheritdoc />
    [Authorize]
    public async Task UpdateAsync(UpdateChatApplicationInput input)
    {
        var chatApplication = await chatApplicationRepository.FindAsync(input.Id);

        input.Name = chatApplication?.Name;
        mapper.Map(input, chatApplication);

        await chatApplicationRepository.UpdateAsync(chatApplication);
    }

    /// <inheritdoc />
    [Authorize]
    public async Task<PaginatedListBase<ChatApplicationDto>> GetListAsync(int page, int pageSize)
    {
        var result = await chatApplicationRepository.GetListAsync(page, pageSize, UserContext.GetUserId<Guid>());

        var total = await chatApplicationRepository.GetCountAsync(UserContext.GetUserId<Guid>());

        return new PaginatedListBase<ChatApplicationDto>()
        {
            Result = mapper.Map<List<ChatApplicationDto>>(result),
            Total = total
        };
    }

    /// <inheritdoc />
    [Authorize]
    public async Task<ChatApplicationDto> GetAsync(string id)
    {
        var result = await chatApplicationRepository.FindAsync(id);

        return mapper.Map<ChatApplicationDto>(result);
    }

    public async Task<ChatApplicationDto> GetChatShareApplicationAsync(string chatShareId)
    {
        var chatApplication = await chatApplicationRepository.ChatShareApplicationAsync(chatShareId);

        return mapper.Map<ChatApplicationDto>(chatApplication);
    }

    /// <param name="chatId"></param>
    /// <inheritdoc />
    public async Task<List<ChatDialogDto>> GetChatShareDialogAsync(string chatId)
    {
        var result = await chatApplicationRepository.GetChatShareDialogListAsync(chatId);

        return mapper.Map<List<ChatDialogDto>>(result);
    }

    public async Task CreateChatDialogHistoryAsync(CreateChatDialogHistoryInput input)
    {
        var chatDialogHistory = new ChatDialogHistory(input.ChatDialogId,
            input.Content, TokenHelper.ComputeToken(input.Content), input.Current,
            input.Type);

        // 如果有id则设置id
        if (!input.Id.IsNullOrEmpty())
            chatDialogHistory.SetId(input.Id);

        chatDialogHistory.ReferenceFile.AddRange(mapper.Map<List<ReferenceFile>>(input.ReferenceFile));

        await chatApplicationRepository.CreateChatDialogHistoryAsync(chatDialogHistory);
    }

    public async Task<PaginatedListBase<ChatDialogHistoryDto>> GetChatDialogHistoryAsync(string chatDialogId, int page,
        int pageSize)
    {
        var result =
            await chatApplicationRepository.GetChatDialogHistoryListAsync(chatDialogId, page,
                pageSize);

        var total = await chatApplicationRepository.GetChatDialogHistoryCountAsync(chatDialogId);

        var dto = mapper.Map<List<ChatDialogHistoryDto>>(result.OrderBy(x => x.CreationTime));

        return new PaginatedListBase<ChatDialogHistoryDto>()
        {
            Result = dto,
            Total = total
        };
    }

    public async Task<ChatDialogHistoryDto> GetChatDialogHistoryInfoAsync(string historyId)
    {
        var result = await chatApplicationRepository.GetChatDialogHistoryAsync(historyId);

        if (result == null)
        {
            return new ChatDialogHistoryDto();
        }

        return mapper.Map<ChatDialogHistoryDto>(result);
    }

    public async Task RemoveDialogHistoryAsync(string id)
    {
        await chatApplicationRepository.RemoveChatDialogHistoryByIdAsync(id);
    }

    [Authorize]
    public async Task CreateShareAsync(CreateChatShareInput input)
    {
        var share = new ChatShare(input.Name, input.ChatApplicationId, input.Expires,
            input.AvailableToken, input.AvailableQuantity);
        await chatApplicationRepository.CreateChatShareAsync(share);
    }

    [Authorize]
    public async Task<PaginatedListBase<ChatShareDto>> GetChatShareListAsync(string chatApplicationId, int page,
        int pageSize)
    {
        var result = await chatApplicationRepository.GetChatShareListAsync(UserContext.GetUserId<Guid>(),
            chatApplicationId, page, pageSize);

        var total = await chatApplicationRepository.GetChatShareCountAsync(UserContext.GetUserId<Guid>(),
            chatApplicationId);

        return new PaginatedListBase<ChatShareDto>()
        {
            Result = mapper.Map<List<ChatShareDto>>(result),
            Total = total
        };
    }

    [Authorize]
    public async Task RemoveChatShareAsync(string id)
    {
        await chatApplicationRepository.RemoveChatShareAsync(id);

        await chatApplicationRepository.UnitOfWork.SaveChangesAsync();
    }

    [Authorize]
    public async Task<PaginatedListBase<ChatDialogDto>> GetSessionLogDialogAsync(string chatApplicationId, int page,
        int pageSize)
    {
        var result =
            await chatApplicationRepository.GetSessionLogDialogListAsync(UserContext.GetUserId<Guid>(),
                chatApplicationId,
                page,
                pageSize);

        var total = await chatApplicationRepository.GetSessionLogDialogCountAsync(UserContext.GetUserId<Guid>(),
            chatApplicationId);

        return new PaginatedListBase<ChatDialogDto>()
        {
            Result = mapper.Map<List<ChatDialogDto>>(result),
            Total = total
        };
    }

    public async Task PutChatHistoryAsync(PutChatHistoryInput input)
    {
        await chatApplicationRepository.PutChatHistoryAsync(input.Id, input.Content,
            input.ChatShareId);
    }

    [Authorize]
    public async Task PurgeMessageHistoryAsync(string dialogId)
    {
        await chatApplicationRepository.RemovesChatDialogHistoryAsync(dialogId);
    }
}