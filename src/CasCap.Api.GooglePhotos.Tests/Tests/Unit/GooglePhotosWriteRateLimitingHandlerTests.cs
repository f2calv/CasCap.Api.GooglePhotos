using System.Net;
using System.Net.Http.Json;

namespace CasCap.Tests;

/// <summary>Tests client-side temporal rate limiting without Google credentials.</summary>
[Trait("Category", "RateLimiting")]
public sealed class GooglePhotosWriteRateLimitingHandlerTests
{
    [Fact]
    public async Task DisabledLimiterBypassesUnsafeRequests()
    {
        var requestCount = 0;
        using var handler = CreateHandler(
            new GooglePhotosWriteRateLimitOptions(),
            _ =>
            {
                requestCount++;
                return new HttpResponseMessage(HttpStatusCode.OK);
            });
        using var client = new HttpClient(handler);

        using var firstResponse = await SendAsync(client, HttpMethod.Post, TestContext.Current.CancellationToken);
        using var secondResponse = await SendAsync(client, HttpMethod.Post, TestContext.Current.CancellationToken);

        Assert.Equal(2, requestCount);
        Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, secondResponse.StatusCode);
    }

    [Fact]
    public void RegistrationCopiesRateLimitOptions()
    {
        var configuredOptions = CreateEnabledOptions();
        var services = new ServiceCollection();
        services.AddGooglePhotos(new GooglePhotosOptions
        {
            User = "user@example.com",
            ClientId = "client-id",
            ClientSecret = "client-secret",
            WriteRateLimit = configuredOptions
        });
        using var serviceProvider = services.BuildServiceProvider();
        var resolvedOptions = serviceProvider.GetRequiredService<IOptions<GooglePhotosOptions>>().Value;

        configuredOptions.PermitLimit = 2;

        Assert.Equal(1, resolvedOptions.WriteRateLimit.PermitLimit);
    }

    [Fact]
    public void RegistrationCopiesScopes()
    {
        GooglePhotosScope[] scopes = [GooglePhotosScope.AppendOnly];
        var services = new ServiceCollection();
        services.AddGooglePhotos(new GooglePhotosOptions
        {
            User = "user@example.com",
            ClientId = "client-id",
            ClientSecret = "client-secret",
            Scopes = scopes
        });
        using var serviceProvider = services.BuildServiceProvider();
        var resolvedOptions = serviceProvider.GetRequiredService<IOptions<GooglePhotosOptions>>().Value;

        scopes[0] = GooglePhotosScope.PickerMediaItemsReadOnly;

        Assert.Equal(GooglePhotosScope.AppendOnly, Assert.Single(resolvedOptions.Scopes));
    }

    [Fact]
    public void RegistrationRejectsInvalidRateLimit()
    {
        var services = new ServiceCollection();
        services.AddGooglePhotos(new GooglePhotosOptions
        {
            User = "user@example.com",
            ClientId = "client-id",
            ClientSecret = "client-secret",
            WriteRateLimit = new GooglePhotosWriteRateLimitOptions
            {
                Enabled = true,
                SegmentsPerWindow = 2,
                WindowSeconds = 1
            }
        });
        using var serviceProvider = services.BuildServiceProvider();

        Assert.Throws<OptionsValidationException>(
            () => serviceProvider.GetRequiredService<IOptions<GooglePhotosOptions>>().Value);
    }

    [Fact]
    public void RegistrationRejectsNullRateLimit()
    {
        var services = new ServiceCollection();
        services.AddGooglePhotos(new GooglePhotosOptions
        {
            User = "user@example.com",
            ClientId = "client-id",
            ClientSecret = "client-secret",
            WriteRateLimit = null!
        });
        using var serviceProvider = services.BuildServiceProvider();

        Assert.Throws<OptionsValidationException>(
            () => serviceProvider.GetRequiredService<IOptions<GooglePhotosOptions>>().Value);
    }

    [Fact]
    public void RegistrationRejectsNullScopes()
    {
        var services = new ServiceCollection();
        services.AddGooglePhotos(new GooglePhotosOptions
        {
            User = "user@example.com",
            ClientId = "client-id",
            ClientSecret = "client-secret",
            Scopes = null!
        });
        using var serviceProvider = services.BuildServiceProvider();

        Assert.Throws<OptionsValidationException>(
            () => serviceProvider.GetRequiredService<IOptions<GooglePhotosOptions>>().Value);
    }

    [Fact]
    public async Task QueuedWriteHonorsCancellation()
    {
        var requestCount = 0;
        using var handler = CreateHandler(
            CreateEnabledOptions(queueLimit: 1),
            _ =>
            {
                requestCount++;
                return new HttpResponseMessage(HttpStatusCode.OK);
            });
        using var client = new HttpClient(handler);
        using var firstResponse = await SendAsync(client, HttpMethod.Post, TestContext.Current.CancellationToken);
        using var cancellationTokenSource = new CancellationTokenSource();

        var pendingRequest = SendAsync(client, HttpMethod.Post, cancellationTokenSource.Token);
        cancellationTokenSource.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => pendingRequest);
        Assert.Equal(1, requestCount);
    }

    [Fact]
    public async Task ReadOnlySearchBypassesLimiter()
    {
        var requestCount = 0;
        using var handler = CreateHandler(
            CreateEnabledOptions(),
            _ =>
            {
                requestCount++;
                return new HttpResponseMessage(HttpStatusCode.OK);
            });
        using var client = new HttpClient(handler);

        using var firstResponse = await SendAsync(client, HttpMethod.Post, TestContext.Current.CancellationToken, "mediaItems:search");
        using var secondResponse = await SendAsync(client, HttpMethod.Post, TestContext.Current.CancellationToken, "mediaItems:search");

        Assert.Equal(2, requestCount);
        Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, secondResponse.StatusCode);
    }

    [Theory]
    [InlineData("GET")]
    [InlineData("HEAD")]
    [InlineData("OPTIONS")]
    [InlineData("TRACE")]
    public async Task SafeMethodBypassesLimiter(string method)
    {
        var requestCount = 0;
        using var handler = CreateHandler(
            CreateEnabledOptions(),
            _ =>
            {
                requestCount++;
                return new HttpResponseMessage(HttpStatusCode.OK);
            });
        using var client = new HttpClient(handler);
        var httpMethod = new HttpMethod(method);

        using var firstResponse = await SendAsync(client, httpMethod, TestContext.Current.CancellationToken);
        using var secondResponse = await SendAsync(client, httpMethod, TestContext.Current.CancellationToken);

        Assert.Equal(2, requestCount);
        Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, secondResponse.StatusCode);
    }

    [Theory]
    [InlineData("POST")]
    [InlineData("PUT")]
    [InlineData("PATCH")]
    [InlineData("DELETE")]
    public async Task UnsafeMethodConsumesPermit(string method)
    {
        var requestCount = 0;
        using var handler = CreateHandler(
            CreateEnabledOptions(),
            _ =>
            {
                requestCount++;
                return new HttpResponseMessage(HttpStatusCode.OK);
            });
        using var client = new HttpClient(handler);
        var httpMethod = new HttpMethod(method);

        using var firstResponse = await SendAsync(client, httpMethod, TestContext.Current.CancellationToken);
        using var secondResponse = await SendAsync(client, httpMethod, TestContext.Current.CancellationToken);

        Assert.Equal(1, requestCount);
        Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);
        Assert.Equal(HttpStatusCode.TooManyRequests, secondResponse.StatusCode);
        Assert.NotNull(secondResponse.Headers.RetryAfter);
        var error = await secondResponse.Content.ReadFromJsonAsync<Error>(TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.TooManyRequests, (HttpStatusCode?)error?.ErrorStatus?.Code);
        Assert.Equal("RESOURCE_EXHAUSTED", error?.ErrorStatus?.StatusName);
    }

    private static GooglePhotosWriteRateLimitOptions CreateEnabledOptions(int queueLimit = 0)
        => new()
        {
            Enabled = true,
            PermitLimit = 1,
            QueueLimit = queueLimit,
            SegmentsPerWindow = 1,
            WindowSeconds = 60
        };

    private static GooglePhotosWriteRateLimitingHandler CreateHandler(
        GooglePhotosWriteRateLimitOptions rateLimitOptions,
        Func<HttpRequestMessage, HttpResponseMessage> responseFactory)
        => new(Options.Create(new GooglePhotosOptions { WriteRateLimit = rateLimitOptions }))
        {
            InnerHandler = new StubHttpMessageHandler(responseFactory)
        };

    private static async Task<HttpResponseMessage> SendAsync(
        HttpClient client,
        HttpMethod method,
        CancellationToken cancellationToken,
        string requestUri = "test")
    {
        using var request = new HttpRequestMessage(method, $"https://photoslibrary.googleapis.com/v1/{requestUri}");
        return await client.SendAsync(request, cancellationToken);
    }

    private sealed class StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responseFactory) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(responseFactory(request));
    }
}