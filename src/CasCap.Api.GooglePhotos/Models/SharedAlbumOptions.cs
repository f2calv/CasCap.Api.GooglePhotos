namespace CasCap.Models;

/// <summary>Defines the options for a shared album.</summary>
public sealed class SharedAlbumOptions
{
    /// <summary>Gets or sets whether collaborators can add media items.</summary>
    [JsonPropertyName("isCollaborative")]
    public bool IsCollaborative { get; set; }

    /// <summary>Gets or sets whether owners and collaborators can add comments.</summary>
    [JsonPropertyName("isCommentable")]
    public bool IsCommentable { get; set; }
}
