namespace CasCap.Tests;

/// <summary>
/// Exercises Google Photos operations against the configured test account.
/// </summary>
[Trait("Category", "Integration")]
public sealed class GooglePhotosIntegrationTests(ITestOutputHelper output) : TestBase(output)
{
    [SkipIfCIBuildFact]
    public async Task DoLogin()
    {
        var loginResult = await LoginAsync(TestContext.Current.CancellationToken);
        Assert.True(loginResult);
    }

    private async Task<bool> LoginAsync(CancellationToken cancellationToken)
    {
        if (IsCI())
        {
            var accessToken = Environment.GetEnvironmentVariable("GOOGLE_PHOTOS_ACCESS_TOKEN");
            ArgumentException.ThrowIfNullOrWhiteSpace(accessToken);
            //Both clients share one credential provider, so a single call covers them.
            _googlePhotosSvc.SetAuth("Bearer", accessToken);
            return true;
        }

        return await _googlePhotosSvc.LoginAsync(cancellationToken);
    }

    private static bool IsCI() => Environment.GetEnvironmentVariable("TF_BUILD") is not null
        || Environment.GetEnvironmentVariable("GITHUB_ACTIONS") is not null;

    private static string GetRandomAlbumName() => $"integration-{DateTime.UtcNow:yyyyMMdd-HHmmss}-{Guid.NewGuid():N}";

    private async Task<MediaItem> CreateMediaItemAsync(string fileName, CancellationToken cancellationToken)
    {
        var created = await _googlePhotosSvc.UploadSingle(
            Path.Combine(_testFolder, fileName),
            cancellationToken: cancellationToken);
        Assert.NotNull(created);
        Assert.NotNull(created.MediaItem);
        Assert.False(string.IsNullOrWhiteSpace(created.MediaItem.Id));

        var mediaItem = await _googlePhotosSvc.GetMediaItemByIdAsync(created.MediaItem.Id, cancellationToken);
        Assert.NotNull(mediaItem);
        return mediaItem;
    }

    [Fact, Trait("Type", nameof(GooglePhotosPickerService))]
    public async Task PickerSessionLifecycle()
    {
        var loginResult = await LoginAsync(TestContext.Current.CancellationToken);
        Assert.True(loginResult);

        var session = await _googlePhotosPickerSvc.CreateSessionAsync(
            maxItemCount: 1,
            cancellationToken: TestContext.Current.CancellationToken);
        try
        {
            Assert.False(string.IsNullOrWhiteSpace(session.Id));
            Assert.True(Uri.TryCreate(session.PickerUri, UriKind.Absolute, out _));
            Assert.True(session.ExpireTime > DateTimeOffset.UtcNow);

            var retrievedSession = await _googlePhotosPickerSvc.GetSessionAsync(session.Id, TestContext.Current.CancellationToken);
            Assert.Equal(session.Id, retrievedSession.Id);
        }
        finally
        {
            await _googlePhotosPickerSvc.DeleteSessionAsync(session.Id, TestContext.Current.CancellationToken);
        }
    }

    [Theory, Trait("Type", nameof(GooglePhotosService))]
    [InlineData(GooglePhotosUploadMethod.Simple)]
    [InlineData(GooglePhotosUploadMethod.ResumableSingle)]
    [InlineData(GooglePhotosUploadMethod.ResumableMultipart)]
    public async Task UploadMedia(GooglePhotosUploadMethod uploadMethod)
    {
        var loginResult = await LoginAsync(TestContext.Current.CancellationToken);
        Assert.True(loginResult);

        var path = Path.Combine(_testFolder, "test0.jpg");
        var uploadToken = await _googlePhotosSvc.UploadMediaAsync(path, uploadMethod, cancellationToken: TestContext.Current.CancellationToken);
        Assert.False(string.IsNullOrWhiteSpace(uploadToken));
        var newMediaItemResult = await _googlePhotosSvc.AddMediaItemAsync(uploadToken, path, cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(newMediaItemResult);
        Assert.NotNull(newMediaItemResult.MediaItem);
        Assert.False(string.IsNullOrWhiteSpace(newMediaItemResult.MediaItem.Id));
    }

    [Theory]
    [InlineData("test1.jpg", "test2.jpg")]
    [InlineData("test1.jpg", "Урок-английского-10.jpg")]
    public async Task UploadSingle(string file1, string file2)
    {
        var loginResult = await LoginAsync(TestContext.Current.CancellationToken);
        Assert.True(loginResult);

        //upload single media item
        var mediaItem1a = await _googlePhotosSvc.UploadSingle(Path.Combine(_testFolder, file1), cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(mediaItem1a);
        Assert.NotNull(mediaItem1a.MediaItem);
        Assert.False(string.IsNullOrWhiteSpace(mediaItem1a.MediaItem.Id));

        //retrieve single media item by unique id
        var mediaItem1b = await _googlePhotosSvc.GetMediaItemByIdAsync(mediaItem1a.MediaItem.Id, TestContext.Current.CancellationToken);
        Assert.NotNull(mediaItem1b);
        Assert.Equal(mediaItem1a.MediaItem.Id, mediaItem1b.Id);

        //get or create new album
        var albumName = GetRandomAlbumName();
        var album = await _googlePhotosSvc.GetOrCreateAlbumAsync(albumName, cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(album);
        Assert.NotNull(album.Id);

        //upload single media item, assign to above album
        var mediaItem2a = await _googlePhotosSvc.UploadSingle(Path.Combine(_testFolder, file2), album.Id, cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(mediaItem2a);
        Assert.NotNull(mediaItem2a.MediaItem);
        Assert.False(string.IsNullOrWhiteSpace(mediaItem2a.MediaItem.Id));

        //retrieve all media items from album
        var albumMediaItems = await _googlePhotosSvc.GetMediaItemsByAlbumAsync(album.Id, cancellationToken: TestContext.Current.CancellationToken).ToListAsync(TestContext.Current.CancellationToken);
        Assert.NotNull(albumMediaItems);
        Assert.Single(albumMediaItems);
    }

    [Fact]
    public async Task UploadMultiple()
    {
        var loginResult = await LoginAsync(TestContext.Current.CancellationToken);
        Assert.True(loginResult);

        //upload multiple media items
        var filePaths1 = new[] { Path.Combine(_testFolder, "test3.jpg"), Path.Combine(_testFolder, "test4.jpg") };
        var response1 = await _googlePhotosSvc.UploadMultiple(filePaths1, cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(response1);
        Assert.Equal(filePaths1.Length, response1.NewMediaItemResults.Count);

        //get or create new album
        var albumName = GetRandomAlbumName();
        var album = await _googlePhotosSvc.GetOrCreateAlbumAsync(albumName, cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(album);
        Assert.Equal(albumName, album.Title);

        //upload multiple media items, assign to album
        var filePaths2 = new[] { Path.Combine(_testFolder, "test5.jpg"), Path.Combine(_testFolder, "test6.jpg") };
        var response2 = await _googlePhotosSvc.UploadMultiple(filePaths2, album.Id, cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(response2);
        Assert.Equal(filePaths2.Length, response2.NewMediaItemResults.Count);

        var removed = await _googlePhotosSvc.RemoveMediaItemsFromAlbumAsync(album.Id, response2.NewMediaItemResults.Select(p => p.MediaItem.Id).ToArray(), TestContext.Current.CancellationToken);
        Assert.True(removed);
        var added = await _googlePhotosSvc.AddMediaItemsToAlbumAsync(album.Id, response2.NewMediaItemResults.Select(p => p.MediaItem.Id).ToArray(), TestContext.Current.CancellationToken);
        Assert.True(added);

        //retrieve all albums
        var albums = await _googlePhotosSvc.GetAlbumsAsync(cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(albums);
        Assert.Contains(albums, p => p.Title == albumName);
        foreach (var alb in albums.Where(p => p.Title == albumName))
        {
            _output.WriteLine($"Album {alb.Id} contains {alb.MediaItemsCount} media items.");
            //retrieve all media items in each album
            var albumMediaItems = await _googlePhotosSvc.GetMediaItemsByAlbumAsync(alb.Id, cancellationToken: TestContext.Current.CancellationToken).ToListAsync(TestContext.Current.CancellationToken);
            Assert.Equal(alb.MediaItemsCount, albumMediaItems.Count);
            var i = 1;
            foreach (var mediaItem in albumMediaItems)
            {
                _output.WriteLine($"Media item {i}: {mediaItem.MediaMetadata.Width}x{mediaItem.MediaMetadata.Height}");
                i++;
            }
        }

        //retrieve all media items
        var mediaItems = await _googlePhotosSvc.GetMediaItemsAsync(cancellationToken: TestContext.Current.CancellationToken).ToListAsync(TestContext.Current.CancellationToken);
        Assert.True(mediaItems.Count >= filePaths1.Length + filePaths2.Length, "Uploaded media items were not returned by the API.");

        //retrieve multiple media items by unique ids
        var ids = mediaItems.Select(p => p.Id).ToList();
        ids.Add("invalid-id");
        var mediaItems2 = await _googlePhotosSvc.GetMediaItemsByIdsAsync(ids.ToArray(), cancellationToken: TestContext.Current.CancellationToken).ToListAsync(TestContext.Current.CancellationToken);
        Assert.Equal(1, ids.Count - mediaItems2.Count);//should have 1 failed item
        _output.WriteLine($"Retrieved {mediaItems2.Count} of {ids.Count} requested media items.");
    }

    [Fact]
    public async Task FilterMediaItems()
    {
        var loginResult = await LoginAsync(TestContext.Current.CancellationToken);
        Assert.True(loginResult);
        var mediaItem = await CreateMediaItemAsync("test0.jpg", TestContext.Current.CancellationToken);

        var filter = new Filter
        {
            MediaTypeFilter = new MediaTypeFilter
            {
                MediaTypes = [GooglePhotosMediaType.Photo]
            }
        };
        var searchResults = await _googlePhotosSvc.GetMediaItemsByFilterAsync(filter, cancellationToken: TestContext.Current.CancellationToken).ToListAsync(TestContext.Current.CancellationToken);
        Assert.Contains(searchResults, result => result.Id == mediaItem.Id);
        _output.WriteLine($"Filter returned {searchResults.Count} media items.");
    }

    [Fact]
    public async Task AddEnrichments()
    {
        var loginResult = await LoginAsync(TestContext.Current.CancellationToken);
        Assert.True(loginResult);

        var path = Path.Combine(_testFolder, "test7.jpg");
        //upload image
        var uploadToken = await _googlePhotosSvc.UploadMediaAsync(path, cancellationToken: TestContext.Current.CancellationToken);
        Assert.False(string.IsNullOrWhiteSpace(uploadToken));

        //make a mediaItem (but no album)
        var mediaItem = await _googlePhotosSvc.AddMediaItemAsync(uploadToken, path, "my test description", cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(mediaItem);
        Assert.NotNull(mediaItem.MediaItem);

        //get or create new album
        var albumName = GetRandomAlbumName();
        var album = await _googlePhotosSvc.GetOrCreateAlbumAsync(albumName, cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(album);
        Assert.Equal(albumName, album.Title);

        //add enrichment
        var enrichmentId1 = await _googlePhotosSvc.AddEnrichmentToAlbumAsync(album.Id,
            new NewEnrichmentItem($"test enrichment {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}"),
            new AlbumPosition { Position = GooglePhotosPositionType.FirstInAlbum },
            TestContext.Current.CancellationToken);
        Assert.NotNull(enrichmentId1);
        Assert.False(string.IsNullOrWhiteSpace(enrichmentId1.Id));

        //add to album
        var result1 = await _googlePhotosSvc.AddMediaItemsToAlbumAsync(album.Id, new[] { mediaItem.MediaItem.Id }, TestContext.Current.CancellationToken);
        Assert.True(result1);

        //add enrichment relative to media item
        var enrichmentId2 = await _googlePhotosSvc.AddEnrichmentToAlbumAsync(album.Id,
            new NewEnrichmentItem("another text enrichment"),
            new AlbumPosition { Position = GooglePhotosPositionType.AfterMediaItem, RelativeMediaItemId = mediaItem.MediaItem.Id },
            TestContext.Current.CancellationToken);
        Assert.NotNull(enrichmentId2);
        Assert.False(string.IsNullOrWhiteSpace(enrichmentId2.Id));
    }

    [Fact]
    public async Task DownloadBytes()
    {
        var loginResult = await LoginAsync(TestContext.Current.CancellationToken);
        Assert.True(loginResult);
        var mediaItem = await CreateMediaItemAsync("test0.jpg", TestContext.Current.CancellationToken);

        var mediaItems = await _googlePhotosSvc.GetMediaItemsAsync(cancellationToken: TestContext.Current.CancellationToken).ToListAsync(TestContext.Current.CancellationToken);
        Assert.Contains(mediaItems, item => item.Id == mediaItem.Id);

        var bytes = await _googlePhotosSvc.DownloadBytes(mediaItem, cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(bytes);
        Assert.NotEmpty(bytes);
    }
}
