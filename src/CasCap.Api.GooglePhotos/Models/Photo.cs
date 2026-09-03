namespace CasCap.Models;

/// <summary>Contains metadata specific to a photo.</summary>
public class Photo : Camera
{
    /// <summary>Gets or sets the focal length of the camera lens.</summary>
    [JsonPropertyName("focalLength")]
    public float FocalLength { get; set; }

    /// <summary>Gets or sets the aperture f-number.</summary>
    [JsonPropertyName("apertureFNumber")]
    public float ApertureFNumber { get; set; }

    /// <summary>Gets or sets the equivalent ISO value.</summary>
    [JsonPropertyName("isoEquivalent")]
    public int IsoEquivalent { get; set; }

    /// <summary>Gets or sets the exposure time.</summary>
    [JsonPropertyName("exposureTime")]
    public string? ExposureTime { get; set; }
}