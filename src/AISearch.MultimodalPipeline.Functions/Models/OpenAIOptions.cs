using System.ComponentModel.DataAnnotations;

namespace AISearch.MultimodalPipeline.Functions.Models;

public class OpenAIOptions
{
    public const string SectionName = "OpenAI";

    [Required]
    public string ResourceUri { get; set; } = string.Empty;

    [Required]
    public string TextEmbeddingModel { get; set; } = string.Empty;

    [Required]
    public string ApiKey { get; set; } = string.Empty;

    [Required]
    public ChatCompletionOptions ChatCompletion { get; set; } = new();
}

public class ChatCompletionOptions
{
    public string ResourceUri { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
}