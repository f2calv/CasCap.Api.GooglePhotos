namespace CasCap.Models.Picker;

/// <summary>Describes Picker metadata specific to a video.</summary>
public sealed record PickerVideoMetadata
{
    /// <summary>Gets the video frame rate.</summary>
    [JsonPropertyName("fps")]
    public double? FramesPerSecond { get; init; }

    /// <summary>Gets the video processing status.</summary>
    [JsonPropertyName("processingStatus")]
    public string? ProcessingStatus { get; init; }
}
