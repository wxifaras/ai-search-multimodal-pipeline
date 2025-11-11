using AISearch.MultimodalPipeline.Functions.Services;
using Azure;
using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Indexes.Models;
using Microsoft.Extensions.Logging;

namespace Services;

public class IndexerService : IIndexerService
{
    private readonly SearchIndexerClient _indexerClient;
    private readonly ILogger<IndexerService> _logger;

    public IndexerService(
        SearchIndexerClient indexerClient,
        ILogger<IndexerService> logger)
    {
        _indexerClient = indexerClient;
        _logger = logger;
    }

    public async Task CreateIndexerAsync(
        string indexerName,
        string dataSourceName,
        string targetIndexName,
        string skillsetName)
    {
        //try
        //{
        //    await _indexerClient.DeleteIndexerAsync(indexerName);
        //    _logger.LogInformation($"Existing indexer '{indexerName}' deleted.");
        //}
        //catch (RequestFailedException ex) when (ex.Status == 404)
        //{
        //    _logger.LogInformation($"No existing indexer '{indexerName}' to delete.");
        //}

        var indexer = new SearchIndexer(indexerName, dataSourceName, targetIndexName)
        {
            SkillsetName = skillsetName,
            Parameters = new IndexingParameters
            {
                MaxFailedItems = -1,
                MaxFailedItemsPerBatch = 0,
                BatchSize = 1,
                Configuration =
                {
                    ["allowSkillsetToReadFileData"] = true
                }
            }
        };

        indexer.FieldMappings.Add(new FieldMapping("metadata_storage_name")
        {
            TargetFieldName = "document_title"
        });

        await _indexerClient.CreateOrUpdateIndexerAsync(indexer);
        _logger.LogInformation($"Indexer '{indexerName}' created or updated successfully.");
    }
}