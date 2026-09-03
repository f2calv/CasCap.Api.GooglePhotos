namespace CasCap.Models;

/// <summary>Contains metadata for a Google Photos media item.</summary>
public class MediaMetadata
{
    /// <summary>Gets or sets when the media item was created.</summary>
    [JsonPropertyName("creationTime")]
    public DateTime CreationTime { get; set; }

    /// <summary>Gets or sets the media width in pixels.</summary>
    [JsonPropertyName("width")]
    public string Width { get; set; } = default!;

    /// <summary>Gets or sets the media height in pixels.</summary>
    [JsonPropertyName("height")]
    public string Height { get; set; } = default!;

    /// <summary>Gets or sets photo-specific metadata.</summary>
    [JsonPropertyName("photo")]
    public Photo? Photo { get; set; }

    /// <summary>Gets or sets video-specific metadata.</summary>
    [JsonPropertyName("video")]
    public Video? Video { get; set; }
}
