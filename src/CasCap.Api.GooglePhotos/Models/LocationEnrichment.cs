namespace CasCap.Models;

/// <summary>Represents an enrichment containing one location.</summary>
/// <remarks><see href="https://developers.google.com/photos/library/reference/rest/v1/albums/addEnrichment#locationenrichment" /></remarks>
public sealed class LocationEnrichment(Location location)
{
    /// <summary>Gets or sets the location for this enrichment item.</summary>
    [JsonPropertyName("location")]
    public Location Location { get; set; } = location;
}