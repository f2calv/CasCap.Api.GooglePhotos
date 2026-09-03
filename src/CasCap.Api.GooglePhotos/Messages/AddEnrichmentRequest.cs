namespace CasCap.Messages;

/// <summary>Represents a request to add an enrichment to an album.</summary>
/// <remarks><see href="https://developers.google.com/photos/library/reference/rest/v1/albums/addEnrichment#request-body" /></remarks>
internal sealed class AddEnrichmentRequest(NewEnrichmentItem newEnrichmentItem, AlbumPosition albumPosition)
{
    /// <summary>Gets or sets the enrichment to add.</summary>
    [JsonPropertyName("newEnrichmentItem")]
    public NewEnrichmentItem NewEnrichmentItem { get; set; } = newEnrichmentItem;

    /// <summary>Gets or sets where the enrichment is inserted.</summary>
    [JsonPropertyName("albumPosition")]
    public AlbumPosition AlbumPosition { get; set; } = albumPosition;
}