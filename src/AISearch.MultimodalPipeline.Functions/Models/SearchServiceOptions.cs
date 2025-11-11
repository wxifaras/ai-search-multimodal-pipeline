using System.ComponentModel.DataAnnotations;

namespace AISearch.MultimodalPipeline.Functions.Models;

public record SearchServiceOptions
{
    public const string SectionName = "SearchService";

    [Required]
    public string Endpoint { get; set; } = string.Empty;

    [Required]
    public string ApiKey { get; set; } = string.Empty;

    [Required]
    public string SkillSetApiVersion { get; set; } = string.Empty;
}