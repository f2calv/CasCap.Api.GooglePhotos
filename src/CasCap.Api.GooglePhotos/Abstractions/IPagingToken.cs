namespace CasCap.Abstractions;

public interface IPagingToken
{
    /// <summary>
    /// A continuation token to get the next page of the results.
    /// </summary>
    string? NextPageToken { get; set; }
}
