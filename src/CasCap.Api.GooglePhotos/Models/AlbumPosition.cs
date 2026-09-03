namespace CasCap.Models;

/// <summary>Defines where an item is inserted into an album.</summary>
/// <remarks><see href="https://developers.google.com/photos/library/reference/rest/v1/AlbumPosition" /></remarks>
public sealed class AlbumPosition
{
    /// <summary>Gets or sets the position type.</summary>
    [JsonPropertyName("position")]
    public GooglePhotosPositionType Position { get; set; }

    /// <summary>Gets or sets the media item after which the new item is inserted.</summary>
    [JsonPropertyName("relativeMediaItemId")]
    public string? RelativeMediaItemId { get; set; }

    /// <summary>Gets or sets the enrichment item after which the new item is inserted.</summary>
    [JsonPropertyName("relativeEnrichmentItemId")]
    public string? RelativeEnrichmentItemId { get; set; }
}