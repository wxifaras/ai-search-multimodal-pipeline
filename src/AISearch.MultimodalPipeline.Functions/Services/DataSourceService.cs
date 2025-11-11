using AISearch.MultimodalPipeline.Functions.Models;
using Azure;
using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Indexes.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AISearch.MultimodalPipeline.Functions.Services;

public class DataSourceService : IDataSourceService
{
    private readonly SearchIndexerClient _indexerClient;
    private readonly BlobStorageOptions _blobOptions;
    private readonly ILogger<DataSourceService> _logger;

    public DataSourceService(
       SearchIndexerClient indexerClient,
       IOptions<BlobStorageOptions> blobOptions,
       ILogger<DataSourceService> logger)
    {
        _indexerClient = indexerClient;
        _blobOptions = blobOptions.Value;
        _logger = logger;
    }

    public async Task CreateBlobDataSourceAsync(string dataSourceName)
    {
        var container = new SearchIndexerDataContainer(_blobOptions.ContainerName);
        var resourceIdConnectionString = $"ResourceId={_blobOptions.ResourceId};";

        var dataSource = new SearchIndexerDataSourceConnection(
            name: dataSourceName,
            type: SearchIndexerDataSourceType.AzureBlob,
            connectionString: resourceIdConnectionString,
            container: container
        )
        {
            Description = "A data source to store multi-modality documents",
            DataChangeDetectionPolicy = new HighWaterMarkChangeDetectionPolicy("metadata_storage_last_modified"),
            DataDeletionDetectionPolicy = new SoftDeleteColumnDeletionDetectionPolicy
            {
                SoftDeleteColumnName = "metadata_storage_is_deleted",
                SoftDeleteMarkerValue = "true"
            }
        };

        await _indexerClient.CreateOrUpdateDataSourceConnectionAsync(dataSource);
        _logger.LogInformation($"Data source '{dataSourceName}' created or updated.");
    }
}