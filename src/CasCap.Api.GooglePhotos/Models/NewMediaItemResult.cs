namespace CasCap.Models;

/// <summary>Represents the result of creating one media item.</summary>
/// <remarks><see href="https://developers.google.com/photos/library/reference/rest/v1/mediaItems/batchCreate#NewMediaItemResult" /></remarks>
public sealed class NewMediaItemResult
{
    /// <summary>Gets or sets the upload token used to create the item.</summary>
    [JsonPropertyName("uploadToken")]
    public string UploadToken { get; set; } = default!;

    /// <summary>Gets or sets the creation status.</summary>
    [JsonPropertyName("status")]
    public Status Status { get; set; } = default!;

    /// <summary>Gets or sets the created media item.</summary>
    [JsonPropertyName("mediaItem")]
    public MediaItem MediaItem { get; set; } = default!;
}