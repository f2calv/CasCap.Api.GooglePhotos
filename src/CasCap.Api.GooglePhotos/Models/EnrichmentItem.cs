namespace CasCap.Models;

/// <summary>Represents an album enrichment item.</summary>
/// <remarks><see href="https://developers.google.com/photos/library/reference/rest/v1/albums/addEnrichment#enrichmentitem" /></remarks>
public class EnrichmentItem
{
    /// <summary>Gets or sets the enrichment item identifier.</summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = default!;
}