using Google.Apis.Auth.OAuth2;

namespace CasCap.Services;

/// <summary>Holds the Google Photos authorization shared by every client in the application.</summary>
/// <remarks>
/// Registered as a singleton so that authorizing once applies to every resolved client, and so that
/// an expiring access token is refreshed for each outgoing request rather than being captured at login.
/// </remarks>
public sealed class GooglePhotosCredentialProvider : IDisposable
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly ILogger<GooglePhotosCredentialProvider> _logger;
    private readonly IOptions<GooglePhotosOptions> _options;
    private UserCredential? _credential;
    private AuthenticationHeaderValue? _suppliedAuthorization;

    /// <summary>Initializes a new instance of the <see cref="GooglePhotosCredentialProvider" /> class.</summary>
    /// <param name="logger">The logger used for authorization diagnostics.</param>
    /// <param name="options">The configured Google Photos options.</param>
    public GooglePhotosCredentialProvider(ILogger<GooglePhotosCredentialProvider> logger, IOptions<GooglePhotosOptions> options)
    {
        _logger = logger;
        _options = options;
    }

    /// <summary>Authorizes the configured user, opening the system browser when no cached grant is usable.</summary>
    /// <param name="cancellationToken">A token that can cancel authorization.</param>
    /// <returns><see langword="true" /> when authorization succeeds; otherwise, <see langword="false" />.</returns>
    /// <exception cref="GooglePhotosException">Thrown when required settings are missing or a scope is unsupported.</exception>
    public async Task<bool> LoginAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var credential = await GooglePhotosAuthorization.AuthorizeAsync(_logger, _options.Value, cancellationToken).ConfigureAwait(false);
            if (credential is null)
                return false;

            _credential = credential;
            _suppliedAuthorization = null;
            return true;
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <summary>Uses an externally acquired access token instead of the interactive OAuth flow.</summary>
    /// <param name="tokenType">The authorization scheme, such as <c>Bearer</c>.</param>
    /// <param name="accessToken">The access token value.</param>
    /// <remarks>The supplied token is not refreshed, so the caller owns its lifetime.</remarks>
    public void SetAuthorization(string tokenType, string accessToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tokenType);
        ArgumentException.ThrowIfNullOrWhiteSpace(accessToken);
        _suppliedAuthorization = new AuthenticationHeaderValue(tokenType, accessToken);
        _credential = null;
    }

    /// <summary>Gets the authorization header to apply to an outgoing request.</summary>
    /// <param name="cancellationToken">A token that can cancel a token refresh.</param>
    /// <returns>The authorization header, or <see langword="null" /> when the application has not authorized.</returns>
    //TODO: expose the access token expiry so a long-running caller can observe it. Refresh itself is already handled
    //per request below, which resolves the original report, but there is still no way to read the remaining lifetime.
    //See https://github.com/f2calv/CasCap.Api.GooglePhotos/issues/119
    public async ValueTask<AuthenticationHeaderValue?> GetAuthorizationAsync(CancellationToken cancellationToken = default)
    {
        if (_suppliedAuthorization is not null)
            return _suppliedAuthorization;

        var credential = _credential;
        if (credential is null)
            return null;

        //Google's credential refreshes the access token in place when it is close to expiry.
        var accessToken = await credential.GetAccessTokenForRequestAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
        return string.IsNullOrWhiteSpace(accessToken) ? null : new AuthenticationHeaderValue("Bearer", accessToken);
    }

    /// <summary>Creates a delegating handler that applies this authorization to outgoing requests.</summary>
    /// <param name="innerHandler">The handler that sends the request once the authorization header is applied.</param>
    /// <returns>A handler suitable for constructing an <see cref="HttpClient" /> without dependency injection.</returns>
    public DelegatingHandler CreateAuthorizationHandler(HttpMessageHandler innerHandler)
        => new GooglePhotosAuthorizationHandler(this) { InnerHandler = innerHandler };

    /// <inheritdoc />
    public void Dispose() => _gate.Dispose();
}
