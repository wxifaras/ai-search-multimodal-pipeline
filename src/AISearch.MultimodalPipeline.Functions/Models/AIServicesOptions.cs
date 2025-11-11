using System.ComponentModel.DataAnnotations;

namespace AISearch.MultimodalPipeline.Functions.Models;

public class AIServicesOptions
{
    public const string SectionName = "AIServices";

    [Required]
    public string CognitiveServicesEndpoint { get; set; } = string.Empty;

    [Required]
    public string CognitiveServicesKey { get; set; } = string.Empty;
}