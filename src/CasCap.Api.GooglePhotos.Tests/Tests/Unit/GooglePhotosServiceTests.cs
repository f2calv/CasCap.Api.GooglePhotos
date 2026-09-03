using System.Net;
using System.Text;
using System.Text.Json;

namespace CasCap.Tests;

/// <summary>Tests Google Photos Library API request construction and orchestration without Google credentials.</summary>
[Trait("Category", "Library")]
public sealed class GooglePhotosServiceTests
{
    [Fact]
    public async Task AddMediaItemsToAlbumDeduplicatesAndBatches()
    {
        var batches = new List<string[]>();
        using var client = CreateClient(async (request, cancellationToken) =>
        {
            Assert.Equal(HttpMethod.Post, request.Method);
            Assert.Equal("/v1/albums/album-id:batchAddMediaItems", request.RequestUri?.AbsolutePath);
            var json = await request.Content!.ReadAsStringAsync(cancellationToken);
            using var document = JsonDocument.Parse(json);
            batches.Add(document.RootElement.GetProperty("mediaItemIds")
                .EnumerateArray()
                .Select(element => element.GetString()!)
                .ToArray());
            return CreateJsonResponse("{}");
        });
        var service = CreateService(client);
        var mediaItemIds = Enumerable.Range(0, 101)
            .Select(index => $"media-{index}")
            .Append("media-0")
            .ToList();

        var result = await service.AddMediaItemsToAlbumAsync(
            "album-id",
            mediaItemIds,
            TestContext.Current.CancellationToken);

        Assert.True(result);
        Assert.Equal([50, 50, 1], batches.Select(batch => batch.Length));
        Assert.Equal(mediaItemIds.Distinct(), batches.SelectMany(batch => batch));
    }

    [Fact]
    public async Task AddMediaItemsToAlbumHonorsCancellation()
    {
        var requestCount = 0;
        using var cancellationTokenSource = new CancellationTokenSource();
        using var client = CreateClient((_, _) =>
        {
            requestCount++;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new CancellationOnDisposeContent("{}", cancellationTokenSource)
            });
        });
        var service = CreateService(client);
        var mediaItemIds = Enumerable.Range(0, 51).Select(index => $"media-{index}").ToList();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => service.AddMediaItemsToAlbumAsync(
            "album-id",
            mediaItemIds,
            cancellationTokenSource.Token));

        Assert.Equal(1, requestCount);
    }

    [Fact]
    public async Task AddMediaItemsToAlbumWrapsApiError()
    {
        using var client = CreateClient((_, _) => Task.FromResult(CreateJsonResponse(
            """{"error":{"code":400,"message":"invalid media IDs","status":"INVALID_ARGUMENT"}}""",
            HttpStatusCode.BadRequest)));
        var service = CreateService(client);

        var exception = await Assert.ThrowsAsync<GooglePhotosException>(() => service.AddMediaItemsToAlbumAsync(
            "album-id",
            new[] { "media-id" },
            TestContext.Current.CancellationToken));

        Assert.Equal("invalid media IDs", exception.Message);
    }

    [Theory]
    [InlineData(GooglePhotosPositionType.FirstInAlbum, null, null, null)]
    [InlineData(GooglePhotosPositionType.AfterMediaItem, "album-id", null, null)]
    [InlineData(GooglePhotosPositionType.AfterMediaItem, "album-id", "media-id", "enrichment-id")]
    public async Task AddMediaItemRejectsInvalidAlbumPosition(
        GooglePhotosPositionType positionType,
        string? albumId,
        string? relativeMediaItemId,
        string? relativeEnrichmentItemId)
    {
        using var client = CreateClient((_, _) => Task.FromResult(CreateJsonResponse("{}")));
        var service = CreateService(client);

        await Assert.ThrowsAsync<NotSupportedException>(() => service.AddMediaItemAsync(
            "upload-token",
            albumId: albumId,
            positionType: positionType,
            relativeMediaItemId: relativeMediaItemId,
            relativeEnrichmentItemId: relativeEnrichmentItemId,
            cancellationToken: TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task DownloadBytesBuildsPhotoParameters()
    {
        Uri? requestUri = null;
        using var client = CreateClient((request, _) =>
        {
            Assert.Equal(HttpMethod.Get, request.Method);
            requestUri = request.RequestUri;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ByteArrayContent([1, 2, 3])
            });
        });
        var service = CreateService(client);
        var mediaItem = new MediaItem
        {
            BaseUrl = "https://example.test/media",
            MediaMetadata = new MediaMetadata { Photo = new Photo() }
        };

        var bytes = await service.DownloadBytes(
            mediaItem,
            maxWidth: 100,
            maxHeight: 200,
            crop: true,
            includeExifMetadata: true,
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal([1, 2, 3], bytes);
        Assert.Equal("https://example.test/media=w100-h200-c-d", requestUri?.AbsoluteUri);
    }

    [Fact]
    public async Task DownloadBytesBuildsVideoParameters()
    {
        Uri? requestUri = null;
        using var client = CreateClient((request, _) =>
        {
            Assert.Equal(HttpMethod.Get, request.Method);
            requestUri = request.RequestUri;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ByteArrayContent([1])
            });
        });
        var service = CreateService(client);
        var mediaItem = new MediaItem
        {
            BaseUrl = "https://example.test/video",
            MediaMetadata = new MediaMetadata { Video = new Video() }
        };

        var bytes = await service.DownloadBytes(
            mediaItem,
            downloadVideoBytes: true,
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal([1], bytes);
        Assert.Equal("https://example.test/video=dv", requestUri?.AbsoluteUri);
    }

    [Fact]
    public async Task DownloadBytesWrapsApiError()
    {
        using var client = CreateClient((_, _) => Task.FromResult(CreateJsonResponse(
            """{"error":{"code":410,"message":"base URL expired","status":"FAILED_PRECONDITION"}}""",
            HttpStatusCode.Gone)));
        var service = CreateService(client);
        var mediaItem = new MediaItem
        {
            BaseUrl = "https://example.test/media",
            MediaMetadata = new MediaMetadata { Photo = new Photo() }
        };

        var exception = await Assert.ThrowsAsync<GooglePhotosException>(() => service.DownloadBytes(
            mediaItem,
            cancellationToken: TestContext.Current.CancellationToken));

        Assert.Equal("base URL expired", exception.Message);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(51)]
    public async Task GetAlbumsRejectsInvalidPageSize(int pageSize)
    {
        using var client = CreateClient((_, _) => Task.FromResult(CreateJsonResponse("{}")));
        var service = CreateService(client);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => service.GetAlbumsAsync(pageSize, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task GetMediaItemsDeduplicatesAcrossPages()
    {
        var requests = new List<Uri>();
        var pagingEvents = new List<PagingEventArgs>();
        using var client = CreateClient((request, _) =>
        {
            requests.Add(request.RequestUri!);
            var responseJson = requests.Count == 1
                ? """{"mediaItems":[{"id":"same","mediaMetadata":{"creationTime":"2026-09-01T00:00:00Z"},"filename":"same.jpg"}],"nextPageToken":"next"}"""
                : """{"mediaItems":[{"id":"same","mediaMetadata":{"creationTime":"2026-09-01T00:00:00Z"},"filename":"same.jpg"},{"id":"new","mediaMetadata":{"creationTime":"2026-09-02T00:00:00Z"},"filename":"new.jpg"}]}""";
            return Task.FromResult(CreateJsonResponse(responseJson));
        });
        var service = CreateService(client);
        service.PagingEvent += (_, args) => pagingEvents.Add(args);

        var mediaItems = await service.GetMediaItemsAsync(cancellationToken: TestContext.Current.CancellationToken)
            .ToListAsync(TestContext.Current.CancellationToken);

        Assert.Equal(["same", "new"], mediaItems.Select(item => item.Id));
        Assert.Equal(2, requests.Count);
        Assert.Contains("pageToken=next", requests[1].Query);
        var pagingEvent = Assert.Single(pagingEvents);
        Assert.Equal(1, pagingEvent.PageNumber);
        Assert.Equal(1, pagingEvent.RecordCount);
    }

    [Fact]
    public async Task GetMediaItemsByFilterRemovesEmptyFilters()
    {
        JsonElement? sentFilter = null;
        using var client = CreateClient(async (request, cancellationToken) =>
        {
            var json = await request.Content!.ReadAsStringAsync(cancellationToken);
            using var document = JsonDocument.Parse(json);
            sentFilter = document.RootElement.GetProperty("filters").Clone();
            return CreateJsonResponse("""{"mediaItems":[]}""");
        });
        var service = CreateService(client);
        var filter = new Filter
        {
            ContentFilter = new ContentFilter
            {
                IncludedContentCategories = [],
                ExcludedContentCategories = []
            },
            DateFilter = new DateFilter { Dates = [], Ranges = [] },
            FeatureFilter = new FeatureFilter { IncludedFeatures = [] },
            MediaTypeFilter = new MediaTypeFilter { MediaTypes = [] }
        };

        _ = await service.GetMediaItemsByFilterAsync(
            filter,
            cancellationToken: TestContext.Current.CancellationToken)
            .ToListAsync(TestContext.Current.CancellationToken);

        Assert.NotNull(sentFilter);
        Assert.False(sentFilter.Value.TryGetProperty("contentFilter", out _));
        Assert.False(sentFilter.Value.TryGetProperty("dateFilter", out _));
        Assert.False(sentFilter.Value.TryGetProperty("featureFilter", out _));
        Assert.False(sentFilter.Value.TryGetProperty("mediaTypeFilter", out _));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    public async Task GetMediaItemsRejectsInvalidPageSize(int pageSize)
    {
        using var client = CreateClient((_, _) => Task.FromResult(CreateJsonResponse("{}")));
        var service = CreateService(client);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(async () =>
            await service.GetMediaItemsAsync(pageSize, cancellationToken: TestContext.Current.CancellationToken)
                .ToListAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task GetOrCreateAlbumCreatesMissingAlbum()
    {
        var requestCount = 0;
        using var client = CreateClient(async (request, cancellationToken) =>
        {
            requestCount++;
            Assert.Equal("/v1/albums", request.RequestUri?.AbsolutePath);
            if (requestCount == 1)
            {
                Assert.Equal(HttpMethod.Get, request.Method);
                return CreateJsonResponse("""{"albums":[]}""");
            }

            Assert.Equal(HttpMethod.Post, request.Method);
            var json = await request.Content!.ReadAsStringAsync(cancellationToken);
            using var document = JsonDocument.Parse(json);
            Assert.Equal("New album", document.RootElement.GetProperty("album").GetProperty("title").GetString());
            return CreateJsonResponse("""{"id":"created-id","title":"New album"}""");
        });
        var service = CreateService(client);

        var album = await service.GetOrCreateAlbumAsync(
            "New album",
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.NotNull(album);
        Assert.Equal("created-id", album.Id);
        Assert.Equal(2, requestCount);
    }

    [Fact]
    public async Task GetOrCreateAlbumReturnsExistingAlbum()
    {
        var requestCount = 0;
        using var client = CreateClient((request, _) =>
        {
            requestCount++;
            Assert.Equal(HttpMethod.Get, request.Method);
            return Task.FromResult(CreateJsonResponse(
                """{"albums":[{"id":"existing-id","title":"Existing album"}]}"""));
        });
        var service = CreateService(client);

        var album = await service.GetOrCreateAlbumAsync(
            "existing ALBUM",
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.NotNull(album);
        Assert.Equal("existing-id", album.Id);
        Assert.Equal(1, requestCount);
    }

    [Theory]
    [InlineData(".jpg", true)]
    [InlineData(".png", true)]
    [InlineData(".mp4", true)]
    [InlineData(".txt", false)]
    public void IsFileUploadableByExtensionClassifiesTypes(string extension, bool expected)
    {
        var actual = GooglePhotosService.IsFileUploadableByExtension(extension);

        Assert.Equal(expected, actual);
    }

    private static HttpClient CreateClient(
        Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> responseFactory)
        => new(new StubHttpMessageHandler(responseFactory))
        {
            BaseAddress = new Uri(RequestUris.BaseAddress)
        };

    private static HttpResponseMessage CreateJsonResponse(string json, HttpStatusCode statusCode = HttpStatusCode.OK)
        => new(statusCode)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };

    private static GooglePhotosService CreateService(HttpClient client)
        => new(
            NullLogger<GooglePhotosService>.Instance,
            Options.Create(new GooglePhotosOptions()),
            client);

    private sealed class CancellationOnDisposeContent(
        string content,
        CancellationTokenSource cancellationTokenSource) : StringContent(content, Encoding.UTF8, "application/json")
    {
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing)
                cancellationTokenSource.Cancel();
        }
    }

    private sealed class StubHttpMessageHandler(
        Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> responseFactory) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
            => responseFactory(request, cancellationToken);
    }
}
