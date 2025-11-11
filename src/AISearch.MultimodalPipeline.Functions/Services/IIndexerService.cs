using Azure.Search.Documents.Indexes.Models;

namespace AISearch.MultimodalPipeline.Functions.Services;

public interface IIndexerService
{
    Task CreateIndexerAsync(string indexerName, string dataSourceName, string targetIndexName, string skillsetName);
    Task RunIndexerAsync(string indexerName, CancellationToken cancellationToken = default);
    Task<SearchIndexerStatus> GetIndexerStatusAsync(string indexerName, CancellationToken cancellationToken = default);
    Task ResetIndexerAsync(string indexerName, CancellationToken cancellationToken = default);
}