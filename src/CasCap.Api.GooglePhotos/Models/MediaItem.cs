namespace CasCap.Models;

/// <summary>Represents a Google Photos media item.</summary>
/// <remarks><see href="https://developers.google.com/photos/library/guides/access-media-items#media-items" /></remarks>
public sealed class MediaItem
{
    /// <summary>Gets or sets the permanent media item identifier.</summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = default!;

    /// <summary>Gets or sets the description shown in Google Photos.</summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>Gets or sets the product URL that can be opened by the user.</summary>
    [JsonPropertyName("productUrl")]
    public string ProductUrl { get; set; } = default!;

    /// <summary>Gets or sets the base URL used to access the media bytes.</summary>
    [JsonPropertyName("baseUrl")]
    public string BaseUrl { get; set; } = default!;

    /// <summary>Gets the time at which this representation was synchronized.</summary>
    [JsonIgnore]
    public DateTime SyncDate { get; } = DateTime.UtcNow;

    /// <summary>Gets whether this item contains photo metadata.</summary>
    [JsonIgnore]
    public bool IsPhoto => MediaMetadata.Photo is not null;

    /// <summary>Gets whether this item contains video metadata.</summary>
    [JsonIgnore]
    public bool IsVideo => MediaMetadata.Video is not null;

    /// <summary>Gets or sets the media MIME type.</summary>
    [JsonPropertyName("mimeType")]
    public string MimeType { get; set; } = default!;

    /// <summary>Gets or sets metadata specific to the underlying media type.</summary>
    [JsonPropertyName("mediaMetadata")]
    public MediaMetadata MediaMetadata { get; set; } = default!;

    /// <summary>Gets or sets the filename shown in Google Photos.</summary>
    [JsonPropertyName("filename")]
    public string Filename { get; set; } = default!;

    /// <inheritdoc />
    public override string ToString() => $"{Filename} {MediaMetadata.CreationTime:yyyy-MM-dd HH:mm:ss}";
}
