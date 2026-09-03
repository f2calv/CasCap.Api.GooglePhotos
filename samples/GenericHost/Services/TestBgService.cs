namespace CasCap.Services;

public sealed class TestBgService(
    ILogger<TestBgService> logger,
    IHostApplicationLifetime appLifetime,
    GooglePhotosService googlePhotosSvc) : BackgroundService
{
    private const string _testFolder = "c:/temp/GooglePhotos/";//local folder of test media files

    protected async override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogDebug("{ClassName} starting {MethodName}...", nameof(TestBgService), nameof(ExecuteAsync));

        //log-in
        if (!await googlePhotosSvc.LoginAsync(stoppingToken)) throw new GooglePhotosException($"login failed!");

        //get existing/create new album
        var albumTitle = $"{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}-{Guid.NewGuid()}";//make-up a random title
        var album = await googlePhotosSvc.GetOrCreateAlbumAsync(albumTitle, cancellationToken: stoppingToken) ?? throw new GooglePhotosException("album creation failed!");
        logger.LogInformation("{ClassName} created album", nameof(TestBgService));

        //upload single media item and assign to album
        var path = $"{_testFolder}test1.jpg";
        _ = await googlePhotosSvc.UploadSingle(path, album.id, cancellationToken: stoppingToken) ?? throw new GooglePhotosException("media item upload failed!");

        //retrieve all media items in the album
        var albumMediaItems = await googlePhotosSvc.GetMediaItemsByAlbumAsync(album.id, cancellationToken: stoppingToken).ToListAsync(stoppingToken);
        logger.LogInformation("{ClassName} retrieved {MediaItemCount} media items", nameof(TestBgService), albumMediaItems.Count);

        logger.LogDebug("{ClassName} exiting {MethodName}...", nameof(TestBgService), nameof(ExecuteAsync));
        appLifetime.StopApplication();
    }
}
