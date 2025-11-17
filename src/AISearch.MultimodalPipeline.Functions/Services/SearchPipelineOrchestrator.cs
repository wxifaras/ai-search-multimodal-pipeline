using AISearch.MultimodalPipeline.Functions.Models;
using Azure;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AISearch.MultimodalPipeline.Functions.Services;

public class SearchPipelineOrchestrator : ISearchPipelineOrchestrator
{
    private readonly IDataSourceService _dataSourceService;
    private readonly ISearchIndexService _searchIndexService;
    private readonly ISkillsetService _skillsetService;
    private readonly IIndexerService _indexerService;
    private readonly ILogger<SearchPipelineOrchestrator> _logger;
    private readonly SearchServiceOptions _searchOptions;

    public SearchPipelineOrchestrator(
        IDataSourceService dataSourceService,
        ISearchIndexService searchIndexService,
        ISkillsetService skillsetService,
        IIndexerService indexerService,
        ILogger<SearchPipelineOrchestrator> logger,
        IOptions<SearchServiceOptions> options)
    {
        _dataSourceService = dataSourceService;
        _searchIndexService = searchIndexService;
        _skillsetService = skillsetService;
        _indexerService = indexerService;
        _logger = logger;
        _searchOptions = options.Value;
    }

    public async Task SetupPipelineAsync()
    {
        await _dataSourceService.CreateBlobDataSourceAsync(_searchOptions.DataSourceName);
        _logger.LogInformation("Blob data source created.");

        await _searchIndexService.CreateSearchIndexAsync(_searchOptions.IndexName);
        _logger.LogInformation("Search index created.");

        await _skillsetService.CreateSkillsetAsync(_searchOptions.SkillsetName, _searchOptions.IndexName);
        _logger.LogInformation("Skillset created.");

        await _indexerService.CreateIndexerAsync(
            _searchOptions.IndexerName, 
            _searchOptions.DataSourceName, 
            _searchOptions.IndexName, 
            _searchOptions.SkillsetName);

        _logger.LogInformation("Indexer created.");
    }

    public async Task RunIndexerAsync()
    {
        _logger.LogInformation("Running indexer for new files...");
        await _indexerService.RunIndexerAsync(_searchOptions.IndexerName);
        _logger.LogInformation("Indexer run initiated.");
    }

    public async Task<bool> IsFirstRunAsync()
    {
        try
        {
            // Try to get indexer status - if it exists, this is not first run
            await _indexerService.GetIndexerStatusAsync("multimodality-indexer");
            _logger.LogInformation("Indexer exists. Not first run.");
            return false;
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            _logger.LogInformation("Indexer not found. This is the first run.");
            return true;
        }
    }
}