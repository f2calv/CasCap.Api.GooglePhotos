namespace CasCap.Models;

/// <summary>Represents an enrichment containing a map between two locations.</summary>
/// <remarks><see href="https://developers.google.com/photos/library/reference/rest/v1/albums/addEnrichment#mapenrichment" /></remarks>
public sealed class MapEnrichment(Location origin, Location destination)
{
    /// <summary>Gets or sets the origin location.</summary>
    [JsonPropertyName("origin")]
    public Location Origin { get; set; } = origin;

    /// <summary>Gets or sets the destination location.</summary>
    [JsonPropertyName("destination")]
    public Location Destination { get; set; } = destination;
}
