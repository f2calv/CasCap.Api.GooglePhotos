namespace CasCap.Models;

/// <summary>Represents text for an album enrichment item.</summary>
/// <remarks><see href="https://developers.google.com/photos/library/reference/rest/v1/albums/addEnrichment#textenrichment" /></remarks>
public sealed class TextEnrichment(string text)
{
    /// <summary>Gets or sets the enrichment text.</summary>
    [JsonPropertyName("text")]
    public string Text { get; set; } = text;
}
