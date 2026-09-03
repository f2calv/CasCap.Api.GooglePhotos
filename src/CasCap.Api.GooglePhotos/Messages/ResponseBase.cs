namespace CasCap.Messages;

/// <summary>Provides a continuation token for paged responses.</summary>
public abstract class ResponseBase : IPagingToken
{
    /// <inheritdoc />
    [JsonPropertyName("nextPageToken")]
    public string? NextPageToken { get; set; }
}
