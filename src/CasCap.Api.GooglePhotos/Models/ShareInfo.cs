namespace CasCap.Models;

/// <summary>Contains sharing information for an album.</summary>
public sealed class ShareInfo
{
    /// <summary>Gets or sets the options that control album sharing.</summary>
    [JsonPropertyName("sharedAlbumOptions")]
    public SharedAlbumOptions SharedAlbumOptions { get; set; } = default!;

    /// <summary>Gets or sets the public URL for the shared album.</summary>
    [JsonPropertyName("shareableUrl")]
    public string ShareableUrl { get; set; } = default!;

    /// <summary>Gets or sets the token used by other users to join the album.</summary>
    [JsonPropertyName("shareToken")]
    public string ShareToken { get; set; } = default!;

    /// <summary>Gets or sets whether the current user has joined the album.</summary>
    [JsonPropertyName("isJoined")]
    public bool IsJoined { get; set; }

    /// <summary>Gets or sets whether the current user owns the album.</summary>
    [JsonPropertyName("isOwned")]
    public bool IsOwned { get; set; }
}
