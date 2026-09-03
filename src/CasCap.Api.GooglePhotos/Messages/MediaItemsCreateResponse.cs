namespace CasCap.Messages;

/// <summary>Represents the result of creating media items.</summary>
public class MediaItemsCreateResponse
{
    /// <summary>Gets or sets the media item creation results.</summary>
    [JsonPropertyName("newMediaItemResults")]
    public List<NewMediaItemResult> NewMediaItemResults { get; set; } = default!;
}