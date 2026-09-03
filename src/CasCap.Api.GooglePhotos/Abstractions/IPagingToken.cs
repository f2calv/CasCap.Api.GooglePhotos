namespace CasCap.Abstractions;

/// <summary>Defines a response that can provide a continuation token for the next page of results.</summary>
public interface IPagingToken
{
    /// <summary>
    /// A continuation token to get the next page of the results.
    /// </summary>
    string? NextPageToken { get; set; }
}
