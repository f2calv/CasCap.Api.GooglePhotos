namespace CasCap.Messages;

/// <summary>Represents a page of media items.</summary>
internal sealed class MediaItemsResponse : ResponseBase
{
    /// <summary>Gets or sets the media items in this page.</summary>
    [JsonPropertyName("mediaItems")]
    public List<MediaItem> MediaItems { get; set; } = default!;
}