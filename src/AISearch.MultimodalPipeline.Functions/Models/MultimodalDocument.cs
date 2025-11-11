using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Indexes.Models;
using System.Text.Json.Serialization;

namespace AISearch.MultimodalPipeline.Functions.Models;

public class MultimodalDocument
{
    [SimpleField(IsKey = true, IsFilterable = true)]
    [JsonPropertyName("content_id")]
    public string ContentId { get; set; }

    [SimpleField(IsFilterable = true)]
    [JsonPropertyName("text_document_id")]
    public string TextDocumentId { get; set; }

    [SearchableField]
    [JsonPropertyName("document_title")]
    public string DocumentTitle { get; set; }

    [SimpleField(IsFilterable = true)]
    [JsonPropertyName("image_document_id")]
    public string ImageDocumentId { get; set; }

    [SearchableField]
    [JsonPropertyName("content_text")]
    public string ContentText { get; set; }

    [VectorSearchField(VectorSearchDimensions = 1024, VectorSearchProfileName = "hnsw")]
    [JsonPropertyName("content_embedding")]
    public float[] ContentEmbedding { get; set; }

    [SimpleField]
    [JsonPropertyName("content_path")]
    public string ContentPath { get; set; }

    // Add other fields like location_metadata if required
}