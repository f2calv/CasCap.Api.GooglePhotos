namespace CasCap.Models;

/// <summary>Defines media-type criteria.</summary>
public sealed class MediaTypeFilter
{
    /// <summary>Gets or sets media types to include.</summary>
    [JsonPropertyName("mediaTypes")]
    public GooglePhotosMediaType[] MediaTypes { get; set; } = default!;
}
