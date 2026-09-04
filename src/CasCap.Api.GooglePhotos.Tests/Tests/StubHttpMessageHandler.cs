namespace CasCap.Tests;

/// <summary>Returns canned responses so tests can exercise HTTP code paths without a network.</summary>
//TODO: superseded by CasCap.Common.Xunit.StubHttpMessageHandler. Delete this once Directory.Packages.props
//moves to a CasCap.Common.Testing version that contains it; the Release build resolves that from NuGet.
internal sealed class StubHttpMessageHandler : HttpMessageHandler
{
    private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _responseFactory;

    internal StubHttpMessageHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> responseFactory)
        => _responseFactory = responseFactory;

    internal StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responseFactory)
        => _responseFactory = (request, _) => Task.FromResult(responseFactory(request));

    /// <inheritdoc />
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        => _responseFactory(request, cancellationToken);
}
