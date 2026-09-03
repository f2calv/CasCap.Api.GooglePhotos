namespace CasCap.Messages;

/// <summary>Represents the result of adding an enrichment to an album.</summary>
/// <remarks><see href="https://developers.google.com/photos/library/reference/rest/v1/albums/addEnrichment#response-body" /></remarks>
internal class AddEnrichmentResponse
{
    /// <summary>Gets or sets the enrichment that was added.</summary>
    [JsonPropertyName("enrichmentItem")]
    public EnrichmentItem EnrichmentItem { get; set; } = default!;
}