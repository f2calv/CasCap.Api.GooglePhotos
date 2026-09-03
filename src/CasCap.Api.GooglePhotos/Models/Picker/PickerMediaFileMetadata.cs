namespace CasCap.Models.Picker;

/// <summary>Describes dimensions, camera information, and type-specific Picker metadata.</summary>
public sealed record PickerMediaFileMetadata
{
    /// <summary>Gets the original width in pixels.</summary>
    [JsonPropertyName("width")]
    public long Width { get; init; }

    /// <summary>Gets the original height in pixels.</summary>
    [JsonPropertyName("height")]
    public long Height { get; init; }

    /// <summary>Gets the camera manufacturer.</summary>
    [JsonPropertyName("cameraMake")]
    public string? CameraMake { get; init; }

    /// <summary>Gets the camera model.</summary>
    [JsonPropertyName("cameraModel")]
    public string? CameraModel { get; init; }

    /// <summary>Gets photo-specific metadata.</summary>
    [JsonPropertyName("photoMetadata")]
    public PickerPhotoMetadata? PhotoMetadata { get; init; }

    /// <summary>Gets video-specific metadata.</summary>
    [JsonPropertyName("videoMetadata")]
    public PickerVideoMetadata? VideoMetadata { get; init; }
}
