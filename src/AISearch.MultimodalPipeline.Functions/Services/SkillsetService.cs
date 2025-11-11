using Azure;
using Azure.Search.Documents.Indexes;
using Microsoft.Extensions.Options;
using AISearch.MultimodalPipeline.Functions.Models;

namespace AISearch.MultimodalPipeline.Functions.Services;

public class SkillsetService : ISkillsetService
{
    private readonly SearchIndexerClient _indexerClient;
    private readonly SearchServiceOptions _searchOptions;
    private readonly OpenAIOptions _openAIOptions;
    private readonly AIServicesOptions _aiServicesOptions;
    private readonly BlobStorageOptions _blobOptions;
    private readonly IHttpClientFactory _httpClientFactory;

    public SkillsetService(
        SearchIndexerClient indexerClient,
        IOptions<SearchServiceOptions> searchOptions,
        IOptions<OpenAIOptions> openAIOptions,
        IOptions<AIServicesOptions> aiServicesOptions,
        IOptions<BlobStorageOptions> blobOptions,
        IHttpClientFactory httpClientFactory)
    {
        _indexerClient = indexerClient;
        _searchOptions = searchOptions.Value;
        _openAIOptions = openAIOptions.Value;
        _aiServicesOptions = aiServicesOptions.Value;
        _blobOptions = blobOptions.Value;
        _httpClientFactory = httpClientFactory;
    }

    public async Task CreateSkillsetAsync(string skillsetName)
    {
        //try
        //{
        //    await _indexerClient.DeleteSkillsetAsync(skillsetName);
        //    Console.WriteLine("Existing skillset deleted.");
        //}
        //catch (RequestFailedException ex) when (ex.Status == 404)
        //{
        //    Console.WriteLine("No existing skillset to delete.");
        //}

        var httpClient = _httpClientFactory.CreateClient();

        //var apiVersion = "2025-08-01-preview";
        var url = $"{_searchOptions.Endpoint}/skillsets/{skillsetName}?api-version={_searchOptions.SkillSetApiVersion}";

        var systemMessage = "You are tasked with generating concise, accurate descriptions of images, figures, diagrams, or charts in documents. The goal is to capture the key information and meaning conveyed by the image without including extraneous details like style, colors, visual aesthetics, or size.\\n\\nInstructions:\\nContent Focus: Describe the core content and relationships depicted in the image.\\n\\nFor diagrams, specify the main elements and how they are connected or interact.\\nFor charts, highlight key data points, trends, comparisons, or conclusions.\\nFor figures or technical illustrations, identify the components and their significance.\\nClarity & Precision: Use concise language to ensure clarity and technical accuracy. Avoid subjective or interpretive statements.\\n\\nAvoid Visual Descriptors: Exclude details about:\\n- Colors, shading, and visual styles.\\n- Image size, layout, or decorative elements.\\n- Fonts, borders, and stylistic embellishments.\\n\\nContext: If relevant, relate the image to the broader content of the technical document or the topic it supports.\\n\\nExample Descriptions:\\nDiagram: \\\"A flowchart showing the four stages of a machine learning pipeline: data collection, preprocessing, model training, and evaluation, with arrows indicating the sequential flow of tasks.\\\"\\n\\nChart: \\\"A bar chart comparing the performance of four algorithms on three datasets, showing that Algorithm A consistently outperforms the others on Dataset 1.\\\"\\n\\nFigure: \\\"A labeled diagram illustrating the components of a transformer model, including the encoder, decoder, self-attention mechanism, and feedforward layers.\\\"";

        var skillsetJson = $$"""
        {
          "name": "{{skillsetName}}",
          "description": "A sample skillset for multi-modality using image verbalization",
          "cognitiveServices": {
            "@odata.type": "#Microsoft.Azure.Search.CognitiveServicesByKey",
            "description": "Cognitive Services resource for AI enrichment",
            "key": "{{_aiServicesOptions.CognitiveServicesKey}}"
          },
          "skills": [
            {
              "@odata.type": "#Microsoft.Skills.Util.DocumentIntelligenceLayoutSkill",
              "name": "document-cracking-skill",
              "description": "Document Layout skill for document cracking",
              "context": "/document",
              "outputMode": "oneToMany",
              "outputFormat": "text",
              "extractionOptions": ["images", "locationMetadata"],
              "chunkingProperties": {     
                  "unit": "characters",
                  "maximumLength": 2000, 
                  "overlapLength": 200
              },
              "inputs": [
                {
                  "name": "file_data",
                  "source": "/document/file_data"
                }
              ],
              "outputs": [
                { 
                  "name": "text_sections", 
                  "targetName": "text_sections" 
                }, 
                { 
                  "name": "normalized_images", 
                  "targetName": "normalized_images" 
                } 
              ]
            },
            {
            "@odata.type": "#Microsoft.Skills.Text.AzureOpenAIEmbeddingSkill",
            "name": "text-embedding-skill",
            "description": "Azure Open AI Embedding skill for text",
            "context": "/document/text_sections/*",
            "inputs": [
                {
                "name": "text",
                "source": "/document/text_sections/*/content"
                }
            ],
            "outputs": [
                {
                "name": "embedding",
                "targetName": "text_vector"
                }
            ],
            "resourceUri": "{{_openAIOptions.ResourceUri}}",
            "deploymentId": "{{_openAIOptions.TextEmbeddingModel}}",
            "apiKey": "{{_openAIOptions.ApiKey}}",
            "dimensions": 3072,
            "modelName": "{{_openAIOptions.TextEmbeddingModel}}"
            },
            {
            "@odata.type": "#Microsoft.Skills.Custom.ChatCompletionSkill",
            "uri": "{{_openAIOptions.ChatCompletion.ResourceUri}}",
            "timeout": "PT230S",
            "apiKey": "{{_openAIOptions.ChatCompletion.ApiKey}}",
            "name": "genAI-prompt-skill",
            "description": "GenAI Prompt skill for image verbalization",
            "context": "/document/normalized_images/*",
            "inputs": [
                {
                "name": "systemMessage",
                "source": "='{{systemMessage}}'"
                },
                {
                "name": "userMessage",
                "source": "='Please describe this image.'"
                },
                {
                "name": "image",
                "source": "/document/normalized_images/*/data"
                }
                ],
                "outputs": [
                    {
                    "name": "response",
                    "targetName": "verbalizedImage"
                    }
                ]
            },    
            {
            "@odata.type": "#Microsoft.Skills.Text.AzureOpenAIEmbeddingSkill",
            "name": "verbalizedImage-embedding-skill",
            "description": "Azure Open AI Embedding skill for verbalized image embedding",
            "context": "/document/normalized_images/*",
            "inputs": [
                {
                "name": "text",
                "source": "/document/normalized_images/*/verbalizedImage",
                "inputs": []
                }
            ],
            "outputs": [
                {
                "name": "embedding",
                "targetName": "verbalizedImage_vector"
                }
            ],
            "resourceUri": "{{_openAIOptions.ResourceUri}}",
            "deploymentId": "{{_openAIOptions.TextEmbeddingModel}}",
            "apiKey": "{{_openAIOptions.ApiKey}}",
            "dimensions": 3072,
            "modelName": "{{_openAIOptions.TextEmbeddingModel}}"
            },
            {
              "@odata.type": "#Microsoft.Skills.Util.ShaperSkill",
              "name": "#5",
              "context": "/document/normalized_images/*",
              "inputs": [
                {
                  "name": "normalized_images",
                  "source": "/document/normalized_images/*",
                  "inputs": []
                },
                {
                  "name": "imagePath",
                  "source": "='{{_blobOptions.ImagesContainerName}}/'+$(/document/normalized_images/*/imagePath)",
                  "inputs": []
                }
              ],
              "outputs": [
                {
                  "name": "output",
                  "targetName": "new_normalized_images"
                }
              ]
            }      
          ], 
           "indexProjections": {
              "selectors": [
                {
                  "targetIndexName": "doc-intelligence-image-verbalization-index",
                  "parentKeyFieldName": "text_document_id",
                  "sourceContext": "/document/text_sections/*",
                  "mappings": [    
                    {
                    "name": "content_embedding",
                    "source": "/document/text_sections/*/text_vector"
                    },                      
                    {
                      "name": "content_text",
                      "source": "/document/text_sections/*/content"
                    },
                    {
                      "name": "location_metadata",
                      "source": "/document/text_sections/*/locationMetadata"
                    },                
                    {
                      "name": "document_title",
                      "source": "/document/document_title"
                    }   
                  ]
                },        
                {
                  "targetIndexName": "doc-intelligence-image-verbalization-index",
                  "parentKeyFieldName": "image_document_id",
                  "sourceContext": "/document/normalized_images/*",
                  "mappings": [    
                    {
                    "name": "content_text",
                    "source": "/document/normalized_images/*/verbalizedImage"
                    },  
                    {
                    "name": "content_embedding",
                    "source": "/document/normalized_images/*/verbalizedImage_vector"
                    },                                           
                    {
                      "name": "content_path",
                      "source": "/document/normalized_images/*/new_normalized_images/imagePath"
                    },                    
                    {
                      "name": "document_title",
                      "source": "/document/document_title"
                    },
                    {
                      "name": "location_metadata",
                      "source": "/document/normalized_images/*/locationMetadata"
                    }             
                  ]
                }
              ],
              "parameters": {
                "projectionMode": "skipIndexingParentDocuments"
              }
          },  
          "knowledgeStore": {
            "storageConnectionString": "ResourceId={{_blobOptions.ResourceId}}",
            "projections": [
              {
                "files": [
                  {
                    "storageContainer": "{{_blobOptions.ImagesContainerName}}",
                    "source": "/document/normalized_images/*"
                  }
                ]
              }
            ]
          }
        }
        """;

        var content = new StringContent(skillsetJson, System.Text.Encoding.UTF8, "application/json");
        httpClient.DefaultRequestHeaders.Add("api-key", _searchOptions.ApiKey);

        var response = await httpClient.PutAsync(url, content);

        if (response.IsSuccessStatusCode)
        {
            Console.WriteLine($"Skillset '{skillsetName}' created or updated successfully via REST API.");
        }
        else
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            throw new Exception($"Failed to create skillset. Status: {response.StatusCode}, Error: {errorContent}");
        }
    }
}