namespace CasCap.Models;

/// <summary>Contains metadata specific to a video.</summary>
public class Video : Camera
{
    /// <summary>Gets or sets the video frame rate.</summary>
    [JsonPropertyName("fps")]
    public double Fps { get; set; }

    /// <summary>Gets or sets the video processing status.</summary>
    [JsonPropertyName("status")]
    public string Status { get; set; } = default!;
}