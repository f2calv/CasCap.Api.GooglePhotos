namespace CasCap.Messages;

/// <summary>Represents the result of retrieving multiple media items by identifier.</summary>
internal sealed class MediaItemsGetResponse
{
    /// <summary>Gets or sets the individual media item results.</summary>
    [JsonPropertyName("mediaItemResults")]
    public List<MediaItemGetResponse> MediaItemResults { get; set; } = default!;
}
