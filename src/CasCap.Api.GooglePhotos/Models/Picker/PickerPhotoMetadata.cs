namespace CasCap.Models.Picker;

/// <summary>Describes Picker metadata specific to a photo.</summary>
public sealed record PickerPhotoMetadata
{
    /// <summary>Gets the focal length in millimeters.</summary>
    [JsonPropertyName("focalLength")]
    public double? FocalLength { get; init; }

    /// <summary>Gets the aperture f-number.</summary>
    [JsonPropertyName("apertureFNumber")]
    public double? ApertureFNumber { get; init; }

    /// <summary>Gets the ISO equivalent.</summary>
    [JsonPropertyName("isoEquivalent")]
    public int? IsoEquivalent { get; init; }

    /// <summary>Gets the exposure duration.</summary>
    [JsonPropertyName("exposureTime")]
    public string? ExposureTime { get; init; }
}