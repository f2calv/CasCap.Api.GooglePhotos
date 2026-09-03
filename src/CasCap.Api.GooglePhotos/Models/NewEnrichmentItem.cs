namespace CasCap.Models;

/// <summary>A new enrichment item to add to an album. Only one enrichment property can be set.</summary>
public sealed class NewEnrichmentItem
{
    /// <summary>Initializes a text enrichment.</summary>
    public NewEnrichmentItem(string text)
    {
        TextEnrichment = new TextEnrichment(text);
    }

    /// <summary>Initializes a location enrichment.</summary>
    public NewEnrichmentItem(string locationName, double latitude, double longitude)
    {
        LocationEnrichment = new LocationEnrichment(new Location(locationName, new LatLng(latitude, longitude)));
    }

    /// <summary>Initializes a map enrichment.</summary>
    public NewEnrichmentItem(Location origin, Location destination)
    {
        MapEnrichment = new MapEnrichment(origin, destination);
    }

    /// <summary>Gets or sets the text to add to the album.</summary>
    [JsonPropertyName("textEnrichment")]
    public TextEnrichment? TextEnrichment { get; set; }

    /// <summary>Gets or sets the location to add to the album.</summary>
    [JsonPropertyName("locationEnrichment")]
    public LocationEnrichment? LocationEnrichment { get; set; }

    /// <summary>Gets or sets the map to add to the album.</summary>
    [JsonPropertyName("mapEnrichment")]
    public MapEnrichment? MapEnrichment { get; set; }
}