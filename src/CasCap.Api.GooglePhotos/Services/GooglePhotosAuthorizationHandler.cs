namespace CasCap.Services;

/// <summary>Applies the shared Google Photos authorization to each outgoing request.</summary>
internal sealed class GooglePhotosAuthorizationHandler(GooglePhotosCredentialProvider credentialProvider) : DelegatingHandler
{
    /// <inheritdoc />
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        //A caller-supplied header, including one copied from HttpClient.DefaultRequestHeaders, always wins.
        request.Headers.Authorization ??= await credentialProvider.GetAuthorizationAsync(cancellationToken).ConfigureAwait(false);
        return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
    }
}
