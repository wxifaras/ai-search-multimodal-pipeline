namespace AISearch.MultimodalPipeline.Functions.Services;

public interface ISearchIndexService
{
    Task CreateSearchIndexAsync(string indexName);
}