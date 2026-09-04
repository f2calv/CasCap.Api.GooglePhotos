namespace CasCap.Models;

/// <summary>Represents a Google Photos album.</summary>
[JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
public sealed class Album
{
    /// <summary>
    /// Identifier for the album. This is a persistent identifier that can be used between sessions to identify this album.
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = default!;

    /// <summary>
    /// Name of the album displayed to the user in their Google Photos account. This string shouldn't be more than 500 characters.
    /// </summary>
    [JsonPropertyName("title")]
    public string Title { get; set; } = default!;

    /// <summary>
    /// [Output only] Google Photos URL for the album. The user needs to be signed in to their Google Photos account to access this link.
    /// </summary>
    [JsonPropertyName("productUrl")]
    public string ProductUrl { get; set; } = default!;

    /// <summary>
    /// [Output only] A URL to the cover photo's bytes. This shouldn't be used as is. Parameters should be appended to this URL before use. See the developer documentation for a complete list of supported parameters. For example, '=w2048-h1024' sets the dimensions of the cover photo to have a width of 2048 px and height of 1024 px.
    /// </summary>
    [JsonPropertyName("coverPhotoBaseUrl")]
    public string CoverPhotoBaseUrl { get; set; } = default!;

    /// <summary>
    /// [Output only] Identifier for the media item associated with the cover photo.
    /// </summary>
    [JsonPropertyName("coverPhotoMediaItemId")]
    public string CoverPhotoMediaItemId { get; set; } = default!;

    /// <summary>
    /// [Output only] True if you can create media items in this album. This field is based on the scopes granted and permissions of the album. If the scopes are changed or permissions of the album are changed, this field is updated.
    /// </summary>
    [JsonPropertyName("isWriteable")]
    public bool IsWriteable { get; set; }

    /// <summary>
    /// [Output only] The number of media items in the album.
    /// </summary>
    [JsonPropertyName("mediaItemsCount")]
    public long? MediaItemsCount { get; set; }

    /// <inheritdoc/>
    public override string ToString() => $"{Title}, {MediaItemsCount} media items";
}
