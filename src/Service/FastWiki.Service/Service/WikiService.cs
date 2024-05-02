using System.Diagnostics;
using System.Web;
using FastWiki.Service.Backgrounds;

namespace FastWiki.Service.Service;

/// <summary>
/// 知识库服务
/// </summary>
public sealed class WikiService(
    WikiMemoryService wikiMemoryService,
    IFileStorageRepository fileStorageRepository,
    IWikiRepository wikiRepository
) : ApplicationService<WikiService>, IWikiService
{
    /// <inheritdoc />
    [Authorize]
    public async Task CreateAsync(CreateWikiInput input)
    {
        var wiki = new Wiki(input.Icon, input.Name, input.Model, input.EmbeddingModel);
        await wikiRepository.AddAsync(wiki);
        
        await wikiRepository.UnitOfWork.SaveChangesAsync();
    }

    /// <inheritdoc />
    [Authorize]
    public async Task<WikiDto> GetAsync(long id)
    {
        var wiki = await wikiRepository.FindAsync(id);

        if (wiki == null)
        {
            throw new UserFriendlyException("知识库不存在");
        }

        return wiki.Map<WikiDto>();
    }

    [Authorize]
    public async Task UpdateAsync(WikiDto dto)
    {
        var wiki = Mapper.Map<Wiki>(dto);
        await wikiRepository.UpdateAsync(wiki);
        
        await wikiRepository.UnitOfWork.SaveChangesAsync();
    }

    /// <inheritdoc />
    [Authorize]
    public async Task<PaginatedListBase<WikiDto>> GetWikiListAsync(string? keyword, int page, int pageSize)
    {
        var wikis = await wikiRepository.GetListAsync(UserContext.GetUserId<Guid>(), keyword, page, pageSize);

        var count = await wikiRepository.GetCountAsync(UserContext.GetUserId<Guid>(), keyword);

        return new PaginatedListBase<WikiDto>()
        {
            Result = wikis.Map<List<WikiDto>>(),
            Total = count
        };
    }

    /// <inheritdoc />
    [Authorize]
    public async Task RemoveAsync(long id)
    {
        var wikiDetailsQuery = await GetWikiDetailsAsync(id, null, string.Empty, 1, int.MaxValue);

        await wikiRepository.RemoveAsync(id);

        var ids = wikiDetailsQuery.Result.Select(x => x.Id).ToList();

        await wikiRepository.RemoveDetailsAsync(ids);

        foreach (var i in ids)
        {
            try
            {
                var memoryServerless = wikiMemoryService.CreateMemoryServerless();
                await memoryServerless.DeleteDocumentAsync(i.ToString(), "wiki");
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }
    }

    /// <inheritdoc />
    [Authorize]
    public async Task CreateWikiDetailsAsync(CreateWikiDetailsInput input)
    {
        var wikiDetail = new WikiDetail(input.WikiId, input.Name, input.FilePath,
            input.FileId, 0, "file");
        wikiDetail.TrainingPattern = input.TrainingPattern;
        wikiDetail.Mode = input.Mode;
        wikiDetail.MaxTokensPerLine = input.MaxTokensPerLine;
        wikiDetail.MaxTokensPerParagraph = input.MaxTokensPerParagraph;
        wikiDetail.OverlappingTokens = input.OverlappingTokens;
        wikiDetail.QAPromptTemplate = input.QAPromptTemplate;

        wikiDetail = await wikiRepository.AddDetailsAsync(wikiDetail);


        await QuantizeBackgroundService.AddWikiDetailAsync(wikiDetail);
    }

    [Authorize]
    public async Task CreateWikiDetailWebPageInputAsync(CreateWikiDetailWebPageInput input)
    {
        var wikiDetail = new WikiDetail(input.WikiId, input.Name, input.Path,
            -1, 0, "web");
        wikiDetail.OverlappingTokens = input.OverlappingTokens;
        wikiDetail.MaxTokensPerLine = input.MaxTokensPerLine;
        wikiDetail.MaxTokensPerParagraph = input.MaxTokensPerParagraph;
        wikiDetail.Mode = input.Mode;
        wikiDetail.TrainingPattern = input.TrainingPattern;

        wikiDetail = await wikiRepository.AddDetailsAsync(wikiDetail);
        var quantizeWikiDetail = Mapper.Map<WikiDetail>(wikiDetail);

        await QuantizeBackgroundService.AddWikiDetailAsync(quantizeWikiDetail);
    }

    [Authorize]
    public async Task CreateWikiDetailDataAsync(CreateWikiDetailDataInput input)
    {
        var wikiDetail = new WikiDetail(input.WikiId, input.Name, input.FilePath,
            input.FileId, 0, "data");

        wikiDetail.OverlappingTokens = input.OverlappingTokens;
        wikiDetail.MaxTokensPerLine = input.MaxTokensPerLine;
        wikiDetail.MaxTokensPerParagraph = input.MaxTokensPerParagraph;
        wikiDetail.Mode = input.Mode;
        wikiDetail.TrainingPattern = input.TrainingPattern;

        wikiDetail = await wikiRepository.AddDetailsAsync(wikiDetail);

        var quantizeWikiDetail = Mapper.Map<WikiDetail>(wikiDetail);

        await QuantizeBackgroundService.AddWikiDetailAsync(quantizeWikiDetail);
    }

    [Authorize]
    public async Task<PaginatedListBase<WikiDetailDto>> GetWikiDetailsAsync(long wikiId, WikiQuantizationState? state,
        string? keyword, int page, int pageSize)
    {
        var wikis = await wikiRepository.GetDetailsListAsync(wikiId, state, keyword, page,
            pageSize);

        var count = await wikiRepository.GetDetailsCountAsync(wikiId, state, keyword);

        return new PaginatedListBase<WikiDetailDto>()
        {
            Result = wikis.Map<List<WikiDetailDto>>(),
            Total = count
        };
    }

    [Authorize]
    public async Task RemoveDetailsAsync(long id)
    {
        await wikiRepository.RemoveDetailsAsync(id);

        try
        {
            var memoryServerless = wikiMemoryService.CreateMemoryServerless();
            await memoryServerless.DeleteDocumentAsync(id.ToString(), "wiki");
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }

    [Authorize]
    public async Task<PaginatedListBase<WikiDetailVectorQuantityDto>> GetWikiDetailVectorQuantityAsync(
        string wikiDetailId, int page, int pageSize)
    {
        var memoryServerless = wikiMemoryService.CreateMemoryServerless();
        var memoryDbs = memoryServerless.Orchestrator.GetMemoryDbs();

        var result = new PaginatedListBase<WikiDetailVectorQuantityDto>();

        var dto = new List<WikiDetailVectorQuantityDto>();

        var entity = await wikiRepository.GetDetailsAsync(long.Parse(wikiDetailId));

        result.Total = entity.DataCount;

        foreach (var memoryDb in memoryDbs)
        {
            // 通过pageSize和page获取到最大数量
            var limit = pageSize * page;
            if (limit < 10)
            {
                limit = 10;
            }

            var filter = new MemoryFilter().ByDocument(wikiDetailId);

            int size = 0;
            await foreach (var item in memoryDb.GetListAsync("wiki", new List<MemoryFilter>()
                           {
                               filter
                           }, limit, true))
            {
                size++;
                if (size < pageSize * (page - 1))
                {
                    continue;
                }

                if (size > pageSize * page)
                {
                    break;
                }

                dto.Add(new WikiDetailVectorQuantityDto()
                {
                    Content = item.Payload["text"].ToString() ?? string.Empty,
                    FileId = item.Tags.FirstOrDefault(x => x.Key == "fileId").Value?.FirstOrDefault() ?? string.Empty,
                    Id = item.Id,
                    Index = size,
                    WikiDetailId = item.Tags["wikiDetailId"].FirstOrDefault() ?? string.Empty,
                    Document_Id = item.Tags["__document_id"].FirstOrDefault() ?? string.Empty
                });
            }
        }

        result.Result = dto;

        return result;
    }

    [Authorize]
    public async Task RemoveDetailVectorQuantityAsync(string documentId)
    {
        var memoryServerless = wikiMemoryService.CreateMemoryServerless();
        await memoryServerless.DeleteDocumentAsync(documentId, "wiki");
    }

    [Authorize]
    public async Task<SearchVectorQuantityResult> GetSearchVectorQuantityAsync(long wikiId, string search,
        double minRelevance = 0D)
    {
        var stopwatch = Stopwatch.StartNew();
        var memoryServerless = wikiMemoryService.CreateMemoryServerless();
        var searchResult = await memoryServerless.SearchAsync(search, "wiki",
            new MemoryFilter().ByTag("wikiId", wikiId.ToString()), minRelevance: minRelevance, limit: 5);

        stopwatch.Stop();

        var searchVectorQuantityResult = new SearchVectorQuantityResult();

        searchVectorQuantityResult.ElapsedTime = stopwatch.ElapsedMilliseconds;

        searchVectorQuantityResult.Result = new List<SearchVectorQuantityDto>();

        foreach (var resultResult in searchResult.Results)
        {
            searchVectorQuantityResult.Result.AddRange(resultResult.Partitions.Select(partition =>
                new SearchVectorQuantityDto()
                {
                    Content = partition.Text,
                    DocumentId = resultResult.DocumentId,
                    Relevance = partition.Relevance,
                    FileId = partition.Tags["fileId"].FirstOrDefault() ?? string.Empty
                }));
        }

        var fileIds = new List<long>();
        fileIds.AddRange(searchVectorQuantityResult.Result.Select(x =>
        {
            if (long.TryParse(x.FileId, out var i))
            {
                return i;
            }

            return -1;
        }).Where(x => x > 0));


        var files = await fileStorageRepository.GetListAsync(fileIds.ToArray());

        foreach (var quantityDto in searchVectorQuantityResult.Result)
        {
            var file = files.FirstOrDefault(x => x.Id.ToString() == quantityDto.FileId);
            quantityDto.FullPath = file?.Path;

            quantityDto.FileName = file?.Name;
        }

        return searchVectorQuantityResult;
    }

    [Authorize]
    public async Task RemoveDetailsVectorAsync(string id)
    {
        await wikiRepository.RemoveDetailsVectorAsync("wiki",id);
    }

    [Authorize]
    public async Task RetryVectorDetailAsync(long id)
    {
        var wikiDetail = await wikiRepository.GetDetailsAsync(id);

        if (wikiDetail == null)
        {
            throw new UserFriendlyException("未找到数据");
        }

        await QuantizeBackgroundService.AddWikiDetailAsync(wikiDetail);
    }

    [Authorize]
    public async Task DetailsRenameNameAsync(long id, string name)
    {
        await wikiRepository.DetailsRenameNameAsync(id, name);
    }

    /// <summary>
    /// 量化状态检查
    /// </summary>
    /// <param name="wikiId"></param>
    /// <returns></returns>
    [Authorize]
    public async Task<List<CheckQuantizationStateDto>> CheckQuantizationStateAsync(long wikiId)
    {
        var values = QuantizeBackgroundService.CacheWikiDetails.Values.Where(x => x.Item1.WikiId == wikiId).ToList();

        if (values.Any())
        {
            return values.Select(x => new CheckQuantizationStateDto
            {
                WikiId = x.Item1.WikiId,
                FileName = x.Item1.FileName,
                State = x.Item1.State
            }).ToList();
        }

        return [];
    }
}