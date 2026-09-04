namespace CasCap.Models;

/// <summary>Provides the header names used by the Google Photos upload protocol.</summary>
/// <remarks><see href="https://developers.google.com/photos/library/guides/resumable-uploads" /></remarks>
internal static class UploadHeaders
{
    /// <summary>The prefix shared by every Google Photos upload protocol header.</summary>
    internal const string Prefix = "X-Goog-Upload-";

    internal const string ContentType = $"{Prefix}Content-Type";

    internal const string Protocol = $"{Prefix}Protocol";

    internal const string Command = $"{Prefix}Command";

    internal const string FileName = $"{Prefix}File-Name";

    internal const string RawSize = $"{Prefix}Raw-Size";

    internal const string Url = $"{Prefix}URL";

    internal const string Offset = $"{Prefix}Offset";

    internal const string Status = $"{Prefix}Status";

    internal const string ChunkGranularity = $"{Prefix}Chunk-Granularity";

    internal const string SizeReceived = $"{Prefix}Size-Received";

    /// <summary>Determines whether a request participates in the Google Photos upload protocol.</summary>
    /// <param name="request">The outgoing request to inspect.</param>
    /// <returns><see langword="true" /> when the request carries an upload protocol header; otherwise, <see langword="false" />.</returns>
    internal static bool IsUploadRequest(HttpRequestMessage request)
    {
        foreach (var header in request.Headers)
            if (header.Key.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase))
                return true;
        return false;
    }
}
