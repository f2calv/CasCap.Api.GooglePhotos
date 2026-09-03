namespace CasCap.Models;

/// <summary>Represents a WGS84 latitude and longitude pair in degrees.</summary>
/// <remarks><see href="https://developers.google.com/photos/library/reference/rest/v1/albums/addEnrichment#latlng" /></remarks>
public class LatLng(double latitude, double longitude)
{
    /// <summary>Gets or sets the latitude in the range -90.0 through 90.0.</summary>
    [JsonPropertyName("latitude")]
    public double Latitude { get; set; } = latitude;

    /// <summary>Gets or sets the longitude in the range -180.0 through 180.0.</summary>
    [JsonPropertyName("longitude")]
    public double Longitude { get; set; } = longitude;
}