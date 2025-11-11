using System.ComponentModel.DataAnnotations;

namespace AISearch.MultimodalPipeline.Functions.Models;

public record BlobStorageOptions
{
    public const string SectionName = "BlobStorage";

    [Required]
    public string ResourceId { get; set; } = string.Empty;

    [Required]
    public string ContainerName { get; set; } = string.Empty;

    [Required]
    public string ImagesContainerName { get; set; } = string.Empty;
}