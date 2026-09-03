namespace CasCap.Messages;

/// <summary>Represents a page of albums.</summary>
internal class AlbumsGetResponse : ResponseBase
{
    /// <summary>Gets or sets the albums in this page.</summary>
    [JsonPropertyName("albums")]
    public List<Album>? Albums { get; set; }
}