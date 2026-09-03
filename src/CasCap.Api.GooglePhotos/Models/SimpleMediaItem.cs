namespace CasCap.Models;

/// <summary>Identifies uploaded media bytes for a create request.</summary>
/// <remarks><see href="https://developers.google.com/photos/library/reference/rest/v1/mediaItems/batchCreate#SimpleMediaItem" /></remarks>
public sealed class SimpleMediaItem
{
    /// <summary>Gets or sets the token identifying uploaded media bytes.</summary>
    [JsonPropertyName("uploadToken")]
    public string UploadToken { get; set; } = default!;

    /// <summary>Gets or sets the optional file name shown in Google Photos.</summary>
    [JsonPropertyName("fileName")]
    public string? FileName { get; set; }
}
