namespace CasCap.Models;

/// <summary>Represents a physical location.</summary>
/// <remarks><see href="https://developers.google.com/photos/library/reference/rest/v1/albums/addEnrichment#location" /></remarks>
public sealed class Location(string locationName, LatLng latLng)
{
    /// <summary>Gets or sets the display name of the location.</summary>
    [JsonPropertyName("locationName")]
    public string LocationName { get; set; } = locationName;

    /// <summary>Gets or sets the position of the location on the map.</summary>
    [JsonPropertyName("latlng")]
    public LatLng LatLng { get; set; } = latLng;
}