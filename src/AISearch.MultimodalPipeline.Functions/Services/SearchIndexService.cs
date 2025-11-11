using AISearch.MultimodalPipeline.Functions.Models;
using AISearch.MultimodalPipeline.Functions.Services;
using Azure;
using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Indexes.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AISearch.MultimodalPipeline.Functions.Services;

public class SearchIndexService : ISearchIndexService
{
    private readonly SearchIndexClient _indexClient;
    private readonly OpenAIOptions _openAIOptions;
    private readonly ILogger<SearchIndexService> _logger;

    public SearchIndexService(
        SearchIndexClient indexClient,
        IOptions<OpenAIOptions> openAIOptions,
        ILogger<SearchIndexService> logger)
    {
        _indexClient = indexClient;
        _openAIOptions = openAIOptions.Value;
        _logger = logger;
    }

    public async Task CreateSearchIndexAsync(string indexName)
    {
        var fields = new List<SearchField>
        {
            new SearchField("content_id", SearchFieldDataType.String)
            {
                IsKey = true,
                IsFilterable = true,
                IsSortable = true,
                IsFacetable = false,
                AnalyzerName = LexicalAnalyzerName.Keyword
            },
            new SearchField("text_document_id", SearchFieldDataType.String)
            {
                IsSearchable = false,
                IsFilterable = true,
                IsSortable = false,
                IsFacetable = false
            },
            new SearchField("document_title", SearchFieldDataType.String)
            {
                IsSearchable = true,
                IsFilterable = false,
                IsSortable = false,
                IsFacetable = false
            },
            new SearchField("image_document_id", SearchFieldDataType.String)
            {
                IsSearchable = false,
                IsFilterable = true,
                IsSortable = false,
                IsFacetable = false
            },
            new SearchField("content_text", SearchFieldDataType.String)
            {
                IsSearchable = true,
                IsFilterable = false,
                IsSortable = false,
                IsFacetable = false
            },
            new SearchField("content_embedding", SearchFieldDataType.Collection(SearchFieldDataType.Single))
            {
                IsSearchable = true,
                VectorSearchDimensions = 3072,
                VectorSearchProfileName = "hnsw"
            },
            new SearchField("content_path", SearchFieldDataType.String)
            {
                IsSearchable = false,
                IsFilterable = false,
                IsSortable = false,
                IsFacetable = false
            },
            new ComplexField("location_metadata")
            {
                Fields =
                {
                    new SimpleField("pageNumber", SearchFieldDataType.Int32)
                    {
                        IsFilterable = true,
                        IsSortable = false,
                        IsFacetable = false
                    }
                }
            }
        };

        var vectorSearch = new VectorSearch();

        var hnswAlgorithm = new HnswAlgorithmConfiguration("defaulthnsw")
        {
            Parameters = new HnswParameters
            {
                M = 4,
                EfConstruction = 400,
                Metric = VectorSearchAlgorithmMetric.Cosine
            }
        };
        vectorSearch.Algorithms.Add(hnswAlgorithm);

        var scalarQuantization = new ScalarQuantizationCompression("scalar-quant-8bit");
        vectorSearch.Compressions.Add(scalarQuantization);

        var vectorizer = new AzureOpenAIVectorizer("demo-vectorizer")
        {
            Parameters = new AzureOpenAIVectorizerParameters
            {
                ResourceUri = new Uri(_openAIOptions.ResourceUri),
                DeploymentName = _openAIOptions.TextEmbeddingModel,
                ModelName = _openAIOptions.TextEmbeddingModel,
                ApiKey = _openAIOptions.ApiKey
            }
        };
        vectorSearch.Vectorizers.Add(vectorizer);

        var vectorProfile = new VectorSearchProfile("hnsw", "defaulthnsw")
        {
            VectorizerName = "demo-vectorizer",
            CompressionName = "scalar-quant-8bit"
        };
        vectorSearch.Profiles.Add(vectorProfile);

        var semanticConfig = new SemanticConfiguration("semanticconfig", new SemanticPrioritizedFields
        {
            TitleField = new SemanticField("document_title")
        });

        var semanticSearch = new SemanticSearch
        {
            DefaultConfigurationName = "semanticconfig"
        };
        semanticSearch.Configurations.Add(semanticConfig);

        var index = new SearchIndex(indexName, fields)
        {
            VectorSearch = vectorSearch,
            SemanticSearch = semanticSearch
        };

        await _indexClient.CreateOrUpdateIndexAsync(index);
        _logger.LogInformation($"Index '{indexName}' created or updated successfully.");
    }
}