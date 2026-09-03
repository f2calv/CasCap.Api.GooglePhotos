namespace CasCap.Models;

/// <summary>Contains information about the contributor of a media item.</summary>
public sealed class ContributorInfo
{
    /// <summary>Gets or sets the contributor profile-picture base URL.</summary>
    [JsonPropertyName("profilePictureBaseUrl")]
    public string? ProfilePictureBaseUrl { get; set; }

    /// <summary>Gets or sets the contributor display name.</summary>
    [JsonPropertyName("displayName")]
    public string? DisplayName { get; set; }
}