namespace CasCap.Models;

/// <summary>Defines Google Photos feature criteria.</summary>
public sealed class FeatureFilter
{
    /// <summary>Gets or sets features that media must include.</summary>
    [JsonPropertyName("includedFeatures")]
    public GooglePhotosFeatureType[] IncludedFeatures { get; set; } = default!;
}
