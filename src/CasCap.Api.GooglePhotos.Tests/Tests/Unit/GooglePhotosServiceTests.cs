using System.Text;
using System.Text.Json;

namespace CasCap.Tests;

/// <summary>Tests Google Photos Library API request construction and orchestration without Google credentials.</summary>
[Trait("Category", "Library")]
public sealed class GooglePhotosServiceTests
{
    [Fact]
    public async Task AddMediaItems_BatchesCreationRequests()
    {
        var batchSizes = new List<int>();
        using var client = CreateClient(async (request, cancellationToken) =>
        {
            var json = await request.Content!.ReadAsStringAsync(cancellationToken);
            using var document = JsonDocument.Parse(json);
            batchSizes.Add(document.RootElement.GetProperty("newMediaItems").GetArrayLength());
            return CreateJsonResponse("""{"newMediaItemResults":[]}""");
        });
        var service = CreateService(client);
        var uploadItems = Enumerable.Range(0, 51)
            .Select(index => new UploadItem($"token-{index}", $"file-{index}.jpg"))
            .ToList();

        var response = await service.AddMediaItemsAsync(
            uploadItems,
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.NotNull(response);
        Assert.Equal([50, 1], batchSizes);
    }

    [Fact]
    public async Task GetAlbum_WrapsApiError()
    {
        using var client = CreateClient((_, _) => Task.FromResult(CreateJsonResponse(
            """{"error":{"code":403,"message":"forbidden","status":"PERMISSION_DENIED"}}""",
            HttpStatusCode.Forbidden)));
        var service = CreateService(client);

        var exception = await Assert.ThrowsAsync<GooglePhotosException>(() => service.GetAlbumAsync(
            "album-id",
            TestContext.Current.CancellationToken));

        Assert.Equal("forbidden", exception.Message);
        //The canonical reason must survive so callers can branch without matching on the message.
        Assert.Equal(403, exception.Status?.Code);
        Assert.Equal("PERMISSION_DENIED", exception.Status?.StatusName);
    }

    [Fact]
    public async Task AddMediaItemsToAlbum_DeduplicatesAndBatches()
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
    public async Task AddMediaItemsToAlbum_HonorsCancellation()
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
    public async Task AddMediaItemsToAlbum_WrapsApiError()
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
    public async Task AddMediaItem_RejectsInvalidAlbumPosition(
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
    public async Task DownloadBytes_BuildsPhotoParameters()
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

        var bytes = await service.DownloadBytesAsync(
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
    public async Task DownloadBytes_BuildsVideoParameters()
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

        var bytes = await service.DownloadBytesAsync(
            mediaItem,
            downloadVideoBytes: true,
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal([1], bytes);
        Assert.Equal("https://example.test/video=dv", requestUri?.AbsoluteUri);
    }

    [Fact]
    public async Task DownloadBytes_WrapsApiError()
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

        var exception = await Assert.ThrowsAsync<GooglePhotosException>(() => service.DownloadBytesAsync(
            mediaItem,
            cancellationToken: TestContext.Current.CancellationToken));

        Assert.Equal("base URL expired", exception.Message);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(51)]
    public async Task GetAlbums_RejectsInvalidPageSize(int pageSize)
    {
        using var client = CreateClient((_, _) => Task.FromResult(CreateJsonResponse("{}")));
        var service = CreateService(client);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => service.GetAlbumsAsync(pageSize, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task GetMediaItems_DeduplicatesAcrossPages()
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
    public async Task GetMediaItemsByFilter_RemovesEmptyFilters()
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
    public async Task GetMediaItems_RejectsInvalidPageSize(int pageSize)
    {
        using var client = CreateClient((_, _) => Task.FromResult(CreateJsonResponse("{}")));
        var service = CreateService(client);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(async () =>
            await service.GetMediaItemsAsync(pageSize, cancellationToken: TestContext.Current.CancellationToken)
                .ToListAsync(TestContext.Current.CancellationToken));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(50)]
    [InlineData(100)]
    public async Task GetMediaItems_SendsRequestedPageSize(int pageSize)
    {
        Uri? requestUri = null;
        using var client = CreateClient((request, _) =>
        {
            requestUri = request.RequestUri;
            return Task.FromResult(CreateJsonResponse("""{"mediaItems":[]}"""));
        });
        var service = CreateService(client);

        _ = await service.GetMediaItemsAsync(pageSize, cancellationToken: TestContext.Current.CancellationToken)
            .ToListAsync(TestContext.Current.CancellationToken);

        Assert.Contains($"pageSize={pageSize}", requestUri!.Query);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(25)]
    [InlineData(50)]
    public async Task GetAlbums_SendsRequestedPageSize(int pageSize)
    {
        Uri? requestUri = null;
        using var client = CreateClient((request, _) =>
        {
            requestUri = request.RequestUri;
            return Task.FromResult(CreateJsonResponse("""{"albums":[]}"""));
        });
        var service = CreateService(client);

        _ = await service.GetAlbumsAsync(pageSize, TestContext.Current.CancellationToken);

        Assert.Contains($"pageSize={pageSize}", requestUri!.Query);
    }

    [Theory]
    [InlineData("X-Goog-Upload-Command", true)]
    [InlineData("X-Goog-Upload-Protocol", true)]
    [InlineData("X-Goog-Upload-Content-Type", true)]
    [InlineData("X-Goog-Api-Client", false)]
    public void IsUploadRequest_MatchesProtocolHeaders(string headerName, bool expected)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, RequestUris.BaseAddress);
        request.Headers.Add(headerName, "value");

        var actual = UploadHeaders.IsUploadRequest(request);

        Assert.Equal(expected, actual);
    }

    [Fact]
    public async Task GetOrCreateAlbum_CreatesMissingAlbum()
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
    public async Task GetOrCreateAlbum_ReturnsExistingAlbum()
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
    public void IsFileUploadableByExtension_ClassifiesTypes(string extension, bool expected)
    {
        var actual = GooglePhotosService.IsFileUploadableByExtension(extension);

        Assert.Equal(expected, actual);
    }

    [Fact]
    public async Task Login_RejectsUndefinedScope()
    {
        using var client = CreateClient((_, _) => Task.FromResult(CreateJsonResponse("{}")));
        var service = CreateService(client, new GooglePhotosOptions
        {
            User = "local-user",
            ClientId = "client-id",
            ClientSecret = "client-secret",
            Scopes = [(GooglePhotosScope)int.MaxValue]
        });

        var exception = await Assert.ThrowsAsync<GooglePhotosException>(
            () => service.LoginAsync(TestContext.Current.CancellationToken));

        Assert.Contains("Unsupported Google Photos OAuth scope", exception.Message);
    }

    [Fact]
    public void GetTokenStoreKey_IncludesClientId()
    {
        var scopes = new[] { "scope-b", "scope-a" };

        var first = GooglePhotosAuthorization.GetTokenStoreKey("local-user", "client-a", scopes);
        var reordered = GooglePhotosAuthorization.GetTokenStoreKey("local-user", "client-a", scopes.Reverse());
        var secondClient = GooglePhotosAuthorization.GetTokenStoreKey("local-user", "client-b", scopes);

        Assert.Equal(first, reordered);
        Assert.NotEqual(first, secondClient);
    }

    [Fact]
    public async Task UploadMedia_RecoversFromAcceptedChunk()
    {
        var requestCount = 0;
        var uploadOffsets = new List<string?>();
        using var media = await TempMediaFile.CreateAsync([1, 2, 3, 4], TestContext.Current.CancellationToken);
        using var client = CreateClient(async (request, cancellationToken) =>
        {
            requestCount++;
            if (requestCount == 1)
            {
                var response = CreateJsonResponse(string.Empty);
                response.Headers.Add("X-Goog-Upload-URL", "https://upload.example.test/session");
                response.Headers.Add("X-Goog-Upload-Chunk-Granularity", "2");
                response.Headers.Add("X-Goog-Upload-Status", "active");
                return response;
            }

            var command = request.Headers.GetValues("X-Goog-Upload-Command").Single();
            if (command == "query")
            {
                var response = CreateJsonResponse(string.Empty);
                response.Headers.Add("X-Goog-Upload-Status", "active");
                response.Headers.Add("X-Goog-Upload-Size-Received", "2");
                return response;
            }

            uploadOffsets.Add(request.Headers.GetValues("X-Goog-Upload-Offset").SingleOrDefault());
            _ = await request.Content!.ReadAsByteArrayAsync(cancellationToken);
            return requestCount == 2
                ? CreateJsonResponse(
                    """{"error":{"code":500,"message":"lost response","status":"INTERNAL"}}""",
                    HttpStatusCode.InternalServerError)
                : CreateJsonResponse("upload-token");
        });
        var service = CreateService(client);

        var uploadToken = await service.UploadMediaAsync(
            media.Path,
            GooglePhotosUploadMethod.ResumableMultipart,
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal("upload-token", uploadToken);
        Assert.Equal(["0", "2"], uploadOffsets);
        Assert.Equal(4, requestCount);
    }

    [Fact]
    public async Task UploadMedia_RecoversFromAcceptedFinalChunk()
    {
        var requestCount = 0;
        using var media = await TempMediaFile.CreateAsync([1], TestContext.Current.CancellationToken);
        using var client = CreateClient((request, _) =>
        {
            requestCount++;
            if (requestCount == 1)
            {
                var response = CreateJsonResponse(string.Empty);
                response.Headers.Add("X-Goog-Upload-URL", "https://upload.example.test/session");
                response.Headers.Add("X-Goog-Upload-Chunk-Granularity", "2");
                response.Headers.Add("X-Goog-Upload-Status", "active");
                return Task.FromResult(response);
            }

            var command = request.Headers.GetValues("X-Goog-Upload-Command").Single();
            if (command == "query")
            {
                var response = CreateJsonResponse(string.Empty);
                response.Headers.Add("X-Goog-Upload-Status", "active");
                response.Headers.Add("X-Goog-Upload-Size-Received", "1");
                return Task.FromResult(response);
            }

            return Task.FromResult(requestCount == 2
                ? CreateJsonResponse(
                    """{"error":{"code":500,"message":"lost response","status":"INTERNAL"}}""",
                    HttpStatusCode.InternalServerError)
                : CreateJsonResponse("upload-token"));
        });
        var service = CreateService(client);

        var uploadToken = await service.UploadMediaAsync(
            media.Path,
            GooglePhotosUploadMethod.ResumableMultipart,
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal("upload-token", uploadToken);
        Assert.Equal(4, requestCount);
    }

    [Fact]
    public async Task UploadMedia_RecoversResumableSingle()
    {
        var requestCount = 0;
        using var media = await TempMediaFile.CreateAsync([1, 2], TestContext.Current.CancellationToken);
        using var client = CreateClient((request, _) =>
        {
            requestCount++;
            if (requestCount == 1)
            {
                var response = CreateJsonResponse(string.Empty);
                response.Headers.Add("X-Goog-Upload-URL", "https://upload.example.test/session");
                response.Headers.Add("X-Goog-Upload-Status", "active");
                return Task.FromResult(response);
            }

            var command = request.Headers.GetValues("X-Goog-Upload-Command").Single();
            if (command == "query")
            {
                var response = CreateJsonResponse(string.Empty);
                response.Headers.Add("X-Goog-Upload-Status", "active");
                response.Headers.Add("X-Goog-Upload-Size-Received", "2");
                return Task.FromResult(response);
            }

            return Task.FromResult(requestCount == 2
                ? CreateJsonResponse(
                    """{"error":{"code":500,"message":"lost response","status":"INTERNAL"}}""",
                    HttpStatusCode.InternalServerError)
                : CreateJsonResponse("upload-token"));
        });
        var service = CreateService(client);

        var uploadToken = await service.UploadMediaAsync(
            media.Path,
            GooglePhotosUploadMethod.ResumableSingle,
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal("upload-token", uploadToken);
        Assert.Equal(4, requestCount);
    }

    [Theory]
    [InlineData(GooglePhotosUploadMethod.Simple)]
    [InlineData(GooglePhotosUploadMethod.ResumableSingle)]
    [InlineData(GooglePhotosUploadMethod.ResumableMultipart)]
    public async Task UploadMedia_WrapsMalformedError(GooglePhotosUploadMethod uploadMethod)
    {
        using var media = await TempMediaFile.CreateAsync([1], TestContext.Current.CancellationToken);
        using var client = CreateClient((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.BadGateway)
        {
            Content = new StringContent("not-json", Encoding.UTF8, "text/plain")
        }));
        var service = CreateService(client);

        var exception = await Assert.ThrowsAsync<GooglePhotosException>(() => service.UploadMediaAsync(
            media.Path,
            uploadMethod,
            cancellationToken: TestContext.Current.CancellationToken));

        Assert.Equal("Upload failed with HTTP 502.", exception.Message);
    }

    [Fact]
    public async Task AddEnrichmentToAlbum_ReturnsCreatedItem()
    {
        string? requestJson = null;
        using var client = CreateClient(async (request, cancellationToken) =>
        {
            Assert.Equal("/v1/albums/album-id:addEnrichment", request.RequestUri?.AbsolutePath);
            requestJson = await request.Content!.ReadAsStringAsync(cancellationToken);
            return CreateJsonResponse("""{"enrichmentItem":{"id":"enrichment-id"}}""");
        });
        var service = CreateService(client);

        var enrichmentItem = await service.AddEnrichmentToAlbumAsync(
            "album-id",
            new NewEnrichmentItem("some text"),
            new AlbumPosition { Position = GooglePhotosPositionType.FirstInAlbum },
            TestContext.Current.CancellationToken);

        Assert.Equal("enrichment-id", enrichmentItem?.Id);
        Assert.NotNull(requestJson);
        using var document = JsonDocument.Parse(requestJson);
        Assert.Equal(
            "some text",
            document.RootElement.GetProperty("newEnrichmentItem").GetProperty("textEnrichment").GetProperty("text").GetString());
        Assert.Equal("FIRST_IN_ALBUM", document.RootElement.GetProperty("albumPosition").GetProperty("position").GetString());
    }

    [Fact]
    public async Task RemoveMediaItemsFromAlbum_BatchesRequests()
    {
        var batchSizes = new List<int>();
        using var client = CreateClient(async (request, cancellationToken) =>
        {
            Assert.Equal("/v1/albums/album-id:batchRemoveMediaItems", request.RequestUri?.AbsolutePath);
            var json = await request.Content!.ReadAsStringAsync(cancellationToken);
            using var document = JsonDocument.Parse(json);
            batchSizes.Add(document.RootElement.GetProperty("mediaItemIds").GetArrayLength());
            return CreateJsonResponse("{}");
        });
        var service = CreateService(client);
        var mediaItemIds = Enumerable.Range(0, 51).Select(index => $"media-{index}").ToList();

        var result = await service.RemoveMediaItemsFromAlbumAsync(
            "album-id",
            mediaItemIds,
            TestContext.Current.CancellationToken);

        Assert.True(result);
        Assert.Equal([50, 1], batchSizes);
    }

    [Fact]
    public async Task GetMediaItemsByIds_SkipsFailedResults()
    {
        var requests = new List<Uri>();
        using var client = CreateClient((request, _) =>
        {
            requests.Add(request.RequestUri!);
            return Task.FromResult(CreateJsonResponse(
                """
                {"mediaItemResults":[
                  {"mediaItem":{"id":"good","mediaMetadata":{"creationTime":"2026-09-01T00:00:00Z"},"filename":"good.jpg"}},
                  {"status":{"code":3,"message":"bad id","status":"INVALID_ARGUMENT"}},
                  {"mediaItem":{"id":"good","mediaMetadata":{"creationTime":"2026-09-01T00:00:00Z"},"filename":"good.jpg"}}
                ]}
                """));
        });
        var service = CreateService(client);

        var mediaItems = await service.GetMediaItemsByIdsAsync(
            new[] { "good", "bad", "good" },
            TestContext.Current.CancellationToken)
            .ToListAsync(TestContext.Current.CancellationToken);

        Assert.Equal(["good"], mediaItems.Select(item => item.Id));
        Assert.Contains("mediaItemIds=good", Assert.Single(requests).Query);
    }

    [Fact]
    public async Task GetMediaItemById_WrapsApiError()
    {
        using var client = CreateClient((_, _) => Task.FromResult(CreateJsonResponse(
            """{"error":{"code":404,"message":"not found","status":"NOT_FOUND"}}""",
            HttpStatusCode.NotFound)));
        var service = CreateService(client);

        var exception = await Assert.ThrowsAsync<GooglePhotosException>(
            () => service.GetMediaItemByIdAsync("media-id", TestContext.Current.CancellationToken));

        Assert.Equal("not found", exception.Message);
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

    private static GooglePhotosService CreateService(HttpClient client, GooglePhotosOptions? options = null)
    {
        var configuredOptions = Options.Create(options ?? new GooglePhotosOptions());
        return new GooglePhotosService(
            NullLogger<GooglePhotosService>.Instance,
            configuredOptions,
            new GooglePhotosCredentialProvider(NullLogger<GooglePhotosCredentialProvider>.Instance, configuredOptions),
            client);
    }

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
}
