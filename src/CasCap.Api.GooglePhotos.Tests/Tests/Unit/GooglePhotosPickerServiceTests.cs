using System.Net;
using System.Text;

namespace CasCap.Tests;

/// <summary>Tests Picker API request construction and validation without Google credentials.</summary>
[Trait("Category", "Picker")]
public sealed class GooglePhotosPickerServiceTests
{
    [Theory]
    [InlineData(-1)]
    [InlineData(2001)]
    public async Task CreateSessionRejectsInvalidItemCount(int maxItemCount)
    {
        using var client = CreateClient(_ => new HttpResponseMessage(HttpStatusCode.OK));
        var service = CreateService(client);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => service.CreateSessionAsync(maxItemCount, cancellationToken: TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task CreateSessionRejectsNonVersionFourRequestId()
    {
        using var client = CreateClient(_ => new HttpResponseMessage(HttpStatusCode.OK));
        var service = CreateService(client);

        await Assert.ThrowsAsync<ArgumentException>(
            () => service.CreateSessionAsync(requestId: Guid.Empty, cancellationToken: TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task DeleteSessionWrapsMalformedError()
    {
        using var client = CreateClient(_ => new HttpResponseMessage(HttpStatusCode.BadGateway)
        {
            Content = new StringContent("not-json", Encoding.UTF8, "text/plain")
        });
        var service = CreateService(client);

        var exception = await Assert.ThrowsAsync<GooglePhotosException>(
            () => service.DeleteSessionAsync("session-id", TestContext.Current.CancellationToken));

        Assert.Contains("HTTP 502", exception.Message);
    }

    [Fact]
    public async Task DownloadPhotoIncludesDimensionsAndExif()
    {
        Uri? requestUri = null;
        using var client = CreateClient(request =>
        {
            requestUri = request.RequestUri;
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ByteArrayContent([1, 2, 3])
            };
        });
        var service = CreateService(client);
        var mediaItem = new PickedMediaItem
        {
            Type = PickedMediaItemType.Photo,
            MediaFile = new PickerMediaFile { BaseUrl = "https://example.test/media" }
        };
        await using var destination = new MemoryStream();

        await service.DownloadPhotoAsync(
            mediaItem,
            destination,
            maxWidth: 100,
            maxHeight: 200,
            includeExifMetadata: true,
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal("https://example.test/media=w100-h200-d", requestUri?.AbsoluteUri);
        Assert.Equal([1, 2, 3], destination.ToArray());
    }

    [Fact]
    public async Task DownloadPhotoWrapsMalformedError()
    {
        using var client = CreateClient(_ => new HttpResponseMessage(HttpStatusCode.Gone)
        {
            Content = new StringContent("expired", Encoding.UTF8, "text/plain")
        });
        var service = CreateService(client);
        var mediaItem = new PickedMediaItem
        {
            Type = PickedMediaItemType.Photo,
            MediaFile = new PickerMediaFile { BaseUrl = "https://example.test/media" }
        };
        await using var destination = new MemoryStream();

        var exception = await Assert.ThrowsAsync<GooglePhotosException>(() => service.DownloadPhotoAsync(
            mediaItem,
            destination,
            maxWidth: 100,
            maxHeight: 200,
            cancellationToken: TestContext.Current.CancellationToken));

        Assert.Contains("HTTP 410", exception.Message);
    }

    [Fact]
    public async Task GetMediaItemsFollowsPageToken()
    {
        var requests = new List<Uri>();
        using var client = CreateClient(request =>
        {
            requests.Add(request.RequestUri!);
            var responseJson = requests.Count == 1
                ? """{"mediaItems":[{"id":"one","type":"PHOTO","mediaFile":{}}],"nextPageToken":"next"}"""
                : """{"mediaItems":[{"id":"two","type":"VIDEO","mediaFile":{}}]}""";
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(responseJson, Encoding.UTF8, "application/json")
            };
        });
        var service = CreateService(client);

        var mediaItems = await service.GetMediaItemsAsync("session-id", cancellationToken: TestContext.Current.CancellationToken)
            .ToListAsync(TestContext.Current.CancellationToken);

        Assert.Equal(["one", "two"], mediaItems.Select(item => item.Id));
        Assert.Equal(2, requests.Count);
        Assert.Contains("pageToken=next", requests[1].Query);
    }

    [Fact]
    public void RegistrationRejectsEmptyScopes()
    {
        var services = new ServiceCollection();
        services.AddGooglePhotos(new GooglePhotosOptions
        {
            User = "user@example.com",
            ClientId = "client-id",
            ClientSecret = "client-secret",
            Scopes = []
        });
        using var serviceProvider = services.BuildServiceProvider();

        Assert.Throws<OptionsValidationException>(
            () => serviceProvider.GetRequiredService<IOptions<GooglePhotosOptions>>().Value);
    }

    private static HttpClient CreateClient(Func<HttpRequestMessage, HttpResponseMessage> responseFactory)
        => new(new StubHttpMessageHandler(responseFactory))
        {
            BaseAddress = new Uri(PickerRequestUris.BaseAddress)
        };

    private static GooglePhotosPickerService CreateService(HttpClient client)
        => new(
            NullLogger<GooglePhotosPickerService>.Instance,
            new GooglePhotosCredentialProvider(
                NullLogger<GooglePhotosCredentialProvider>.Instance,
                Options.Create(new GooglePhotosOptions())),
            client);

    private sealed class StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responseFactory) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(responseFactory(request));
    }
}
