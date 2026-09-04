namespace CasCap.Models;

/// <summary>Represents an error response from a Google Photos API.</summary>
public sealed class Error
{
    /// <summary>Gets or sets the returned error status.</summary>
    [JsonPropertyName("error")]
    public Status? ErrorStatus { get; set; }
}
