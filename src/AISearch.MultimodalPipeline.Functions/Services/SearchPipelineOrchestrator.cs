using Microsoft.Extensions.Logging;

namespace AISearch.MultimodalPipeline.Functions.Services;

public class SearchPipelineOrchestrator : ISearchPipelineOrchestrator
{
    private readonly IDataSourceService _dataSourceService;
    private readonly ISearchIndexService _searchIndexService;
    private readonly ISkillsetService _skillsetService;
    private readonly IIndexerService _indexerService;
    private readonly ILogger<SearchPipelineOrchestrator> _logger;

    private const string IndexName = "doc-intelligence-image-verbalization-index";
    private const string DataSourceName = "multimodality-datasource";
    private const string SkillsetName = "doc-intelligence-image-verbalization-skillset";
    private const string IndexerName = "multimodality-indexer";

    public SearchPipelineOrchestrator(
        IDataSourceService dataSourceService,
        ISearchIndexService searchIndexService,
        ISkillsetService skillsetService,
        IIndexerService indexerService,
        ILogger<SearchPipelineOrchestrator> logger)
    {
        _dataSourceService = dataSourceService;
        _searchIndexService = searchIndexService;
        _skillsetService = skillsetService;
        _indexerService = indexerService;
        _logger = logger;
    }

    public async Task SetupPipelineAsync()
    {
        await _dataSourceService.CreateBlobDataSourceAsync(DataSourceName);
        _logger.LogInformation("Blob data source created.");

        await _searchIndexService.CreateSearchIndexAsync(IndexName);
        _logger.LogInformation("Search index created.");

        await _skillsetService.CreateSkillsetAsync(SkillsetName);
        _logger.LogInformation("Skillset created.");

        await _indexerService.CreateIndexerAsync(IndexerName, DataSourceName, IndexName, SkillsetName);
        _logger.LogInformation("Indexer created.");
    }
}