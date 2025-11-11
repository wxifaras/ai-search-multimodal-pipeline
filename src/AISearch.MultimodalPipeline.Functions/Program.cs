using AISearch.MultimodalPipeline.Functions.Models;
using AISearch.MultimodalPipeline.Functions.Services;
using Azure;
using Azure.Search.Documents;
using Azure.Search.Documents.Indexes;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Services;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

builder.Services.AddOptions<AIServicesOptions>()
           .Bind(builder.Configuration.GetSection(AIServicesOptions.SectionName))
           .ValidateDataAnnotations();

builder.Services.AddOptions<BlobStorageOptions>()
           .Bind(builder.Configuration.GetSection(BlobStorageOptions.SectionName))
           .ValidateDataAnnotations();

builder.Services.AddOptions<AzureOpenAIOptions>()
           .Bind(builder.Configuration.GetSection(AzureOpenAIOptions.SectionName))
           .ValidateDataAnnotations();

builder.Services.AddOptions<SearchServiceOptions>()
           .Bind(builder.Configuration.GetSection(SearchServiceOptions.SectionName))
           .ValidateDataAnnotations();

builder.Services.AddSingleton(sp =>
{
    var searchOptions = builder.Configuration.GetSection(SearchServiceOptions.SectionName).Get<SearchServiceOptions>()
        ?? throw new InvalidOperationException("SearchService configuration is missing");

    var credential = new AzureKeyCredential(searchOptions.ApiKey);
    return new SearchIndexClient(new Uri(searchOptions.Endpoint), credential);
});

builder.Services.AddSingleton(sp =>
{
    var searchOptions = builder.Configuration.GetSection(SearchServiceOptions.SectionName).Get<SearchServiceOptions>()
        ?? throw new InvalidOperationException("SearchService configuration is missing");

    var credential = new AzureKeyCredential(searchOptions.ApiKey);
    return new SearchIndexerClient(new Uri(searchOptions.Endpoint), credential);
});

builder.Services.AddSingleton(sp =>
{
    var searchOptions = builder.Configuration.GetSection(SearchServiceOptions.SectionName).Get<SearchServiceOptions>()
        ?? throw new InvalidOperationException("SearchService configuration is missing");

    var credential = new AzureKeyCredential(searchOptions.ApiKey);
    const string indexName = "doc-intelligence-image-verbalization-index";
    return new SearchClient(new Uri(searchOptions.Endpoint), indexName, credential);
});

// Register services
builder.Services.AddHttpClient();
builder.Services.AddSingleton<IDataSourceService, DataSourceService>();
builder.Services.AddSingleton<ISearchIndexService, SearchIndexService>();
builder.Services.AddSingleton<ISkillsetService, SkillsetService>();
builder.Services.AddSingleton<IIndexerService, IndexerService>();
builder.Services.AddSingleton<ISearchPipelineOrchestrator, SearchPipelineOrchestrator>();

builder.Services
    .AddApplicationInsightsTelemetryWorkerService()
    .ConfigureFunctionsApplicationInsights();

builder.Build().Run();