namespace CasCap.Models;

/// <summary>Represents a Google API status.</summary>
/// <remarks><see href="https://developers.google.com/photos/library/reference/rest/v1/Status" /></remarks>
public sealed class Status
{
    /// <summary>Gets or sets the numeric status code.</summary>
    [JsonPropertyName("code")]
    public int Code { get; set; }

    /// <summary>Gets or sets the human-readable status message.</summary>
    [JsonPropertyName("message")]
    public string? Message { get; set; }

    /// <summary>Gets or sets the canonical status name.</summary>
    [JsonPropertyName("status")]
    public string? StatusName { get; set; }

    /// <summary>Gets or sets additional status details.</summary>
    [JsonPropertyName("details")]
    public List<object>? Details { get; set; }
}