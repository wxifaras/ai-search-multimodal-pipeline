# AI Search Multimodal Pipeline

This is a serverless Azure-based solution for processing complex, multimodal documents (text + images) by combining:
Azure AI Search for document ingestion, indexing and vector search, Azure Document Intelligence for layout and image/text extraction, and Azure AI Foundry leveraging OpenAI model deployments for both chat completion (to verbalize image/diagram content) and text embedding (to generate searchable vector representations). The pipeline automatically extracts text and images from documents, generates semantic descriptions of visual content, and produces embeddings for both the free-form text and the verbalized image content — enabling advanced retrieval-augmented generation (RAG) scenarios.

To get image verbalizations, each extracted image is passed to the [GenAI Prompt skill (preview)](https://learn.microsoft.com/en-us/azure/search/cognitive-search-skill-genai-prompt) that calls a chat completion model to generate a concise textual description. These descriptions, along with the original document text, are then embedded into vector representations using Azure OpenAI’s text-embedding-3-large model. The result is a single index containing searchable content from both modalities: text and verbalized images.

> **Based on**: This solution is based on the Microsoft Learn tutorial [Multimodal search using Document Layout and image verbalization](https://learn.microsoft.com/en-us/azure/search/tutorial-document-layout-image-verbalization), reimplemented using the **.NET SDKs** instead of REST APIs for a more production-ready, type-safe approach with automated pipeline orchestration via Azure Functions.

> **Note**: The skillset service uses the REST API directly because it requires preview API version `2025-08-01-preview` for the Chat Completion skill and Document Intelligence Layout skill features. All other components use the Azure AI Search .NET SDK.

## Overview

This solution implements an intelligent document processing pipeline that:

- **Automatically processes documents** uploaded to Azure Blob Storage
- **Extracts text and images** using Azure Document Intelligence Layout Skill
- **Generates AI descriptions** of images, diagrams, and charts
- **Creates vector embeddings** for both text chunks and verbalized images
- **Enables semantic search** across multimodal content using Azure AI Search

### Differences from the Tutorial

While based on the Microsoft Learn tutorial, this implementation offers several enhancements:

- **Azure Functions integration**: Blob-triggered serverless processing instead of manual execution
- **.NET SDK usage**: Leverages `Azure.Search.Documents` SDK for type safety and better maintainability (except for skillset creation which uses REST due to preview API requirements)
- **Dependency Injection**: Modern .NET 9 DI patterns with options validation
- **Automated orchestration**: Detects first run vs. incremental updates automatically
- **Production patterns**: Structured services, interfaces, and configuration management

## Architecture

The solution uses Azure Functions to orchestrate the following pipeline:

1. **Blob Trigger**: Monitors Azure Blob Storage for new document uploads
2. **Data Source**: Connects to Blob Storage container with change detection
3. **Skillset**: Applies AI enrichment through **chaining** together multiple skills:
   - Document Intelligence Layout Skill (text extraction + image normalization)
   - Azure OpenAI Chat Completion Skill (image verbalization)
      - recommend using gpt-5
   - Azure OpenAI Embedding Skills (text and image vectorization)
      - recommend using text-embedding-3-large
4. **Search Index**: Stores enriched content with vector search capabilities
5. **Indexer**: Orchestrates the enrichment pipeline and populates the index

### Key Features

- **Multimodal Search**: Search across both text content and visual elements
- **Smart Image Verbalization**: AI-generated descriptions focus on content meaning, not visual aesthetics
- **Automatic Chunking**: Intelligent text splitting (2000 chars with 200 char overlap)
- **Vector Search**: HNSW algorithm with scalar quantization for efficient similarity search
- **Semantic Search**: Azure AI Search semantic ranking for improved relevance
- **Change Detection**: Incremental indexing using high water mark policy
- **Knowledge Store**: Normalized images persisted to Blob Storage

## Prerequisites

- .NET 9 SDKs
- Azure Subscription with the following resources:
  - Azure AI Search (with semantic search enabled)
  - Azure AI Foundry (with text-embedding-3-large and any chat completion model, recommend gpt-5)
     - Can also deploy Azure Open AI
  - Azure AI services multi-service account (for Document Intelligence)
  - Azure Blob Storage
  - Azure Functions (or local development tools)
 
## Configuration

The solution uses the following configuration sections in `local.settings.json` (or App Settings in Azure):

```json
{
  "IsEncrypted": false,
  "Values": {
    
    // Azure AI Search Settings
    "SearchService__Endpoint": "https://<your-search-service>.search.windows.net",
    "SearchService__ApiKey": "<your-search-api-key>",
    "SearchService__SkillSetApiVersion": "2025-08-01-preview",
    "SearchService__IndexName": "multimodal-content-index",
    "SearchService__DataSourceName": "multimodal-content-datasource",
    "SearchService__SkillsetName": "multimodal-content-skillset",
    "SearchService__IndexerName": "multimodal-content-indexer",
    
    // Blob Storage Settings
    "BlobStorage__ResourceId": "/subscriptions/<subscription-id>/resourceGroups/<rg>/providers/Microsoft.Storage/storageAccounts/<account>",
    "BlobStorage__ContainerName": "samples",
    "BlobStorage__ImagesContainerName": "normalized-images",
    
    // Azure AI Services Settings
    "AIServices__CognitiveServicesEndpoint": "https://<your-cognitive-service>.cognitiveservices.azure.com/",
    "AIServices__CognitiveServicesKey": "<your-cognitive-services-key>",
    
    // Azure AI Foundry OpenAI Settings
    "AzureOpenAI__ResourceUri": "https://<your-openai-resource>.openai.azure.com/",
    "AzureOpenAI__ApiKey": "<your-openai-key>",
    "AzureOpenAI__TextEmbeddingModel": "text-embedding-3-large",
    "AzureOpenAI__ChatCompletion__ResourceUri": "https://<your-openai-resource>.openai.azure.com/openai/deployments/<your-gpt-deployment>/chat/completions?api-version=2025-01-01-preview",
    "AzureOpenAI__ChatCompletion__ApiKey": "<your-openai-key>"
  }
}
```

## How It Works

### First Document Upload

When the first document is uploaded to the `samples` container:

1. **AI Search Infrastructure Setup**: Creates all required Azure AI Search resources:
   - Data source connection to Blob Storage
   - Search index with text and vector fields
   - Skillset with Document Intelligence and Azure OpenAI skills
   - Indexer to orchestrate the pipeline

2. **Document Processing**: Runs the indexer to process the document

### Subsequent Uploads

For subsequent document uploads:

1. **Indexer Run**: Executes the existing indexer to process new documents
2. **Incremental Processing**: Only new or modified documents are processed

### Image Verbalization Prompt

The skillset uses a specialized system prompt to generate concise, content-focused image descriptions:

> "You are tasked with generating concise, accurate descriptions of images, figures, diagrams, or charts in documents. The goal is to capture the key information and meaning conveyed by the image without including extraneous details like style, colors, visual aesthetics, or size."

This ensures descriptions are optimized for semantic search rather than visual description.

## Search Index Schema

The index (`doc-intelligence-image-verbalization-index`) includes:

- `content_id`: Unique identifier (key field)
- `text_document_id`: Parent document ID for text chunks
- `image_document_id`: Parent document ID for images
- `document_title`: Source file name
- `content_text`: Extracted text or verbalized image description
- `content_embedding`: 3072-dimensional vector (text-embedding-3-large)
- `content_path`: Storage path for extracted images
- `location_metadata`: Page number

## Deployment

### Local Development

1. Clone the repository
2. Configure `local.settings.json` with your Azure resource details
3. Run the Azure Function locally: `func start`
4. Upload a PDF or document to the Blob Storage `samples` container

### Azure Deployment

1. Create an Azure Function App (.NET 9, Isolated process)
2. Configure application settings with the required configuration sections
3. Deploy the function code
4. Upload documents to trigger the pipeline

## Dependencies

Key NuGet packages:

- `Azure.Search.Documents` - Azure AI Search .NET SDK
- `Microsoft.Azure.Functions.Worker` - Azure Functions isolated worker (.NET 9)
- `Microsoft.Extensions.Http` - HTTP client factory for REST API calls

## Resources

- [Tutorial: Multimodal search using Document Layout and image verbalization](https://learn.microsoft.com/en-us/azure/search/tutorial-document-layout-image-verbalization) - Original tutorial (REST API-based)
- [Azure AI Search .NET SDK Documentation](https://learn.microsoft.com/dotnet/api/overview/azure/search.documents-readme)
- [Azure AI Search Documentation](https://learn.microsoft.com/azure/search/)
- [Azure Document Intelligence](https://learn.microsoft.com/azure/ai-services/document-intelligence/)
- [Azure OpenAI Service](https://learn.microsoft.com/azure/ai-services/openai/)
- [Multimodal Search Patterns](https://learn.microsoft.com/azure/search/search-get-started-vector-multimodal)

## License

MIT License - see [LICENSE](LICENSE) file for details

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.
