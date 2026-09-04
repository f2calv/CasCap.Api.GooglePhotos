using System.Net;
using System.Net.Http.Headers;

namespace CasCap.Tests;

/// <summary>Tests how the shared credential provider is applied to outgoing requests.</summary>
[Trait("Category", "Authorization")]
public sealed class GooglePhotosAuthorizationTests
{
    [Fact]
    public async Task Handler_AppliesSuppliedAuthorization()
    {
        using var credentialProvider = CreateProvider();
        credentialProvider.SetAuthorization("Bearer", "token-value");
        AuthenticationHeaderValue? observed = null;
        using var client = CreateClient(credentialProvider, request => observed = request.Headers.Authorization);

        using var response = await client.GetAsync("albums", TestContext.Current.CancellationToken);

        Assert.Equal("Bearer", observed?.Scheme);
        Assert.Equal("token-value", observed?.Parameter);
    }

    [Fact]
    public async Task Handler_SendsNoAuthorizationBeforeLogin()
    {
        using var credentialProvider = CreateProvider();
        AuthenticationHeaderValue? observed = null;
        using var client = CreateClient(credentialProvider, request => observed = request.Headers.Authorization);

        using var response = await client.GetAsync("albums", TestContext.Current.CancellationToken);

        Assert.Null(observed);
    }

    [Fact]
    public async Task Handler_KeepsCallerSuppliedAuthorization()
    {
        using var credentialProvider = CreateProvider();
        credentialProvider.SetAuthorization("Bearer", "provider-token");
        AuthenticationHeaderValue? observed = null;
        using var client = CreateClient(credentialProvider, request => observed = request.Headers.Authorization);
        using var request = new HttpRequestMessage(HttpMethod.Get, "albums")
        {
            Headers = { Authorization = new AuthenticationHeaderValue("Bearer", "caller-token") }
        };

        using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        Assert.Equal("caller-token", observed?.Parameter);
    }

    [Fact]
    public async Task Authorization_IsSharedByEveryResolvedClient()
    {
        var services = new ServiceCollection();
        services.AddGooglePhotos(new GooglePhotosOptions
        {
            User = "local-user",
            ClientId = "client-id",
            ClientSecret = "client-secret",
            Scopes = [GooglePhotosScope.AppendOnly, GooglePhotosScope.PickerMediaItemsReadOnly]
        });
        using var serviceProvider = services.BuildServiceProvider();

        //The typed clients are transient, so authorizing through one instance must reach the others.
        serviceProvider.GetRequiredService<GooglePhotosService>().SetAuth("Bearer", "shared-token");
        _ = serviceProvider.GetRequiredService<GooglePhotosService>();
        _ = serviceProvider.GetRequiredService<GooglePhotosPickerService>();

        var authorization = await serviceProvider.GetRequiredService<GooglePhotosCredentialProvider>()
            .GetAuthorizationAsync(TestContext.Current.CancellationToken);

        Assert.Equal("shared-token", authorization?.Parameter);
    }

    private static GooglePhotosCredentialProvider CreateProvider()
        => new(NullLogger<GooglePhotosCredentialProvider>.Instance, Options.Create(new GooglePhotosOptions()));

    private static HttpClient CreateClient(GooglePhotosCredentialProvider credentialProvider, Action<HttpRequestMessage> onRequest)
        => new(credentialProvider.CreateAuthorizationHandler(new StubHttpMessageHandler(request =>
        {
            onRequest(request);
            return new HttpResponseMessage(HttpStatusCode.OK);
        })))
        {
            BaseAddress = new Uri(RequestUris.BaseAddress)
        };
}
