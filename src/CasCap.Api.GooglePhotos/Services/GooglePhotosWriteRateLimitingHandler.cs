using System.Threading.RateLimiting;

namespace CasCap.Services;

/// <summary>Applies optional temporal rate limiting to mutating Google Photos Library API requests.</summary>
/// <param name="options">The Google Photos client options.</param>
internal sealed class GooglePhotosWriteRateLimitingHandler(IOptions<GooglePhotosOptions> options) : DelegatingHandler
{
    private const string RateLimitErrorJson = """{"error":{"code":429,"message":"Client-side Google Photos write rate limit exceeded","status":"RESOURCE_EXHAUSTED"}}""";

    private readonly (RateLimiter? RateLimiter, TimeSpan RetryAfter) _rateLimit = CreateRateLimit(options.Value.WriteRateLimit);

    private static (RateLimiter? RateLimiter, TimeSpan RetryAfter) CreateRateLimit(GooglePhotosWriteRateLimitOptions options)
    {
        if (!options.Enabled)
            return (null, default);

        var retryAfter = TimeSpan.FromSeconds((double)options.WindowSeconds / options.SegmentsPerWindow);
        var rateLimiter = new SlidingWindowRateLimiter(new SlidingWindowRateLimiterOptions
        {
            AutoReplenishment = true,
            PermitLimit = options.PermitLimit,
            QueueLimit = options.QueueLimit,
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            SegmentsPerWindow = options.SegmentsPerWindow,
            Window = TimeSpan.FromSeconds(options.WindowSeconds)
        });
        return (rateLimiter, retryAfter);
    }

    /// <inheritdoc />
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (_rateLimit.RateLimiter is null || IsReadOnly(request))
            return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);

        using var lease = await _rateLimit.RateLimiter.AcquireAsync(permitCount: 1, cancellationToken).ConfigureAwait(false);
        if (lease.IsAcquired)
            return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);

        var response = new HttpResponseMessage(HttpStatusCode.TooManyRequests)
        {
            Content = new StringContent(RateLimitErrorJson, Encoding.UTF8, "application/json"),
            RequestMessage = request,
            ReasonPhrase = "Client-side Google Photos write rate limit exceeded"
        };
        response.Headers.RetryAfter = new RetryConditionHeaderValue(
            lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter) ? retryAfter : _rateLimit.RetryAfter);

        return response;
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing)
            _rateLimit.RateLimiter?.Dispose();

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
