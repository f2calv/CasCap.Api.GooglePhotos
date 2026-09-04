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
    public async Task CreateSession_RejectsInvalidItemCount(int maxItemCount)
    {
        using var client = CreateClient(_ => new HttpResponseMessage(HttpStatusCode.OK));
        var service = CreateService(client);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => service.CreateSessionAsync(maxItemCount, cancellationToken: TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task CreateSession_RejectsNonVersionFourRequestId()
    {
        using var client = CreateClient(_ => new HttpResponseMessage(HttpStatusCode.OK));
        var service = CreateService(client);

        await Assert.ThrowsAsync<ArgumentException>(
            () => service.CreateSessionAsync(requestId: Guid.Empty, cancellationToken: TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task DeleteSession_WrapsMalformedError()
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
    public async Task DeleteSession_EscapesSessionId()
    {
        HttpRequestMessage? observed = null;
        using var client = CreateClient(request =>
        {
            observed = request;
            return new HttpResponseMessage(HttpStatusCode.OK);
        });
        var service = CreateService(client);

        await service.DeleteSessionAsync("sessions/../evil id", TestContext.Current.CancellationToken);

        Assert.Equal(HttpMethod.Delete, observed?.Method);
        Assert.Equal("/v1/sessions/sessions%2F..%2Fevil%20id", observed?.RequestUri?.AbsolutePath);
    }

    [Fact]
    public async Task GetSession_ReturnsSession()
    {
        Uri? requestUri = null;
        using var client = CreateClient(request =>
        {
            requestUri = request.RequestUri;
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    """{"id":"session-id","pickerUri":"https://photos.example.test/pick","mediaItemsSet":true}""",
                    Encoding.UTF8,
                    "application/json")
            };
        });
        var service = CreateService(client);

        var session = await service.GetSessionAsync("session-id", TestContext.Current.CancellationToken);

        Assert.Equal("session-id", session.Id);
        Assert.True(session.MediaItemsSet);
        Assert.Equal("/v1/sessions/session-id", requestUri?.AbsolutePath);
    }

    [Fact]
    public async Task DownloadVideo_RequestsRawBytes()
    {
        Uri? requestUri = null;
        using var client = CreateClient(request =>
        {
            requestUri = request.RequestUri;
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent([7, 8, 9]) };
        });
        var service = CreateService(client);
        var mediaItem = new PickedMediaItem
        {
            Id = "picked-id",
            MediaFile = new PickerMediaFile { BaseUrl = "https://example.test/video" }
        };
        using var destination = new MemoryStream();

        await service.DownloadVideoAsync(mediaItem, destination, TestContext.Current.CancellationToken);

        Assert.Equal("https://example.test/video=dv", requestUri?.AbsoluteUri);
        Assert.Equal([7, 8, 9], destination.ToArray());
    }

    [Theory]
    [InlineData(0, 100)]
    [InlineData(16384, 100)]
    [InlineData(100, 0)]
    [InlineData(100, 16384)]
    public async Task DownloadPhoto_RejectsInvalidDimensions(int maxWidth, int maxHeight)
    {
        using var client = CreateClient(_ => new HttpResponseMessage(HttpStatusCode.OK));
        var service = CreateService(client);
        var mediaItem = new PickedMediaItem
        {
            Id = "picked-id",
            MediaFile = new PickerMediaFile { BaseUrl = "https://example.test/photo" }
        };
        using var destination = new MemoryStream();

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => service.DownloadPhotoAsync(
            mediaItem,
            destination,
            maxWidth,
            maxHeight,
            cancellationToken: TestContext.Current.CancellationToken));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    public async Task GetMediaItems_RejectsInvalidPageSize(int pageSize)
    {
        using var client = CreateClient(_ => new HttpResponseMessage(HttpStatusCode.OK));
        var service = CreateService(client);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(async () =>
            await service.GetMediaItemsAsync("session-id", pageSize, TestContext.Current.CancellationToken)
                .ToListAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task DownloadPhoto_IncludesDimensionsAndExif()
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
    public async Task DownloadPhoto_WrapsMalformedError()
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
    public async Task GetMediaItems_FollowsPageToken()
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
}
