namespace AISearch.MultimodalPipeline.Functions.Services;

public interface IDataSourceService
{
    Task CreateBlobDataSourceAsync(string dataSourceName);
}