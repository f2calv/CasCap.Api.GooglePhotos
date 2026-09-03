namespace CasCap.Messages;

/// <summary>Represents the result of retrieving one media item in a batch.</summary>
internal class MediaItemGetResponse
{
    /// <summary>Gets or sets the retrieved media item.</summary>
    [JsonPropertyName("mediaItem")]
    public MediaItem MediaItem { get; set; } = default!;

    /// <summary>Gets or sets an error status when the media item could not be retrieved.</summary>
    [JsonPropertyName("status")]
    public Status? Status { get; set; }
}