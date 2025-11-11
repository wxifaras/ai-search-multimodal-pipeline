namespace AISearch.MultimodalPipeline.Functions.Services;

public interface IIndexerService
{
    Task CreateIndexerAsync(string indexerName, string dataSourceName, string targetIndexName, string skillsetName);
}