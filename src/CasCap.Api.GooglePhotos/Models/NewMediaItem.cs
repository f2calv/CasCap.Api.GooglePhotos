namespace CasCap.Models;

/// <summary>Defines a media item to create after uploading bytes.</summary>
public sealed class NewMediaItem
{
    /// <summary>Gets or sets the description shown in Google Photos.</summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>Gets or sets the uploaded item metadata.</summary>
    [JsonPropertyName("simpleMediaItem")]
    public SimpleMediaItem SimpleMediaItem { get; set; } = default!;
}