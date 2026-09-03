using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.RateLimiting;

namespace CasCap.Services;

/// <summary>Applies optional temporal rate limiting to mutating Google Photos Library API requests.</summary>
internal sealed class GooglePhotosWriteRateLimitingHandler : DelegatingHandler
{
    private const string RateLimitErrorJson = """{"error":{"code":429,"message":"Client-side Google Photos write rate limit exceeded","status":"RESOURCE_EXHAUSTED"}}""";

    private readonly RateLimiter? _rateLimiter;
    private readonly TimeSpan _retryAfter;

    /// <summary>Initializes a new instance of the <see cref="GooglePhotosWriteRateLimitingHandler" /> class.</summary>
    /// <param name="options">The Google Photos client options.</param>
    public GooglePhotosWriteRateLimitingHandler(IOptions<GooglePhotosOptions> options)
    {
        var rateLimitOptions = options.Value.WriteRateLimit;
        if (!rateLimitOptions.Enabled)
            return;

        _retryAfter = TimeSpan.FromSeconds((double)rateLimitOptions.WindowSeconds / rateLimitOptions.SegmentsPerWindow);
        _rateLimiter = new SlidingWindowRateLimiter(new SlidingWindowRateLimiterOptions
        {
            AutoReplenishment = true,
            PermitLimit = rateLimitOptions.PermitLimit,
            QueueLimit = rateLimitOptions.QueueLimit,
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            SegmentsPerWindow = rateLimitOptions.SegmentsPerWindow,
            Window = TimeSpan.FromSeconds(rateLimitOptions.WindowSeconds)
        });
    }

    /// <inheritdoc />
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (_rateLimiter is null || IsReadOnly(request))
            return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);

        using var lease = await _rateLimiter.AcquireAsync(permitCount: 1, cancellationToken).ConfigureAwait(false);
        if (lease.IsAcquired)
            return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);

        var response = new HttpResponseMessage(HttpStatusCode.TooManyRequests)
        {
            Content = new StringContent(RateLimitErrorJson, Encoding.UTF8, "application/json"),
            RequestMessage = request,
            ReasonPhrase = "Client-side Google Photos write rate limit exceeded"
        };
        response.Headers.RetryAfter = new RetryConditionHeaderValue(
            lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter) ? retryAfter : _retryAfter);

        return response;
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing)
            _rateLimiter?.Dispose();

        base.Dispose(disposing);
    }

    private static bool IsReadOnly(HttpRequestMessage request)
        => request.Method == HttpMethod.Get
            || request.Method == HttpMethod.Head
            || request.Method == HttpMethod.Options
            || request.Method == HttpMethod.Trace
            || request.Method == HttpMethod.Post
                && request.RequestUri?.AbsolutePath.EndsWith(RequestUris.POST_mediaItems_search, StringComparison.Ordinal) == true;
}