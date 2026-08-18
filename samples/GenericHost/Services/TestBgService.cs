namespace CasCap.Services;

public class TestBgService : BackgroundService
{
    private readonly ILogger _logger;
    private readonly IHostApplicationLifetime _appLifetime;
    private readonly GooglePhotosService _googlePhotosSvc;

    private const string _testFolder = "c:/temp/GooglePhotos/";//local folder of test media files

    public TestBgService(ILogger<TestBgService> logger, IHostApplicationLifetime appLifetime,
        GooglePhotosService googlePhotosSvc)
    {
        _logger = logger;
        _appLifetime = appLifetime;
        _googlePhotosSvc = googlePhotosSvc;
    }

    protected async override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogDebug("{ClassName} starting {MethodName}...", nameof(TestBgService), nameof(ExecuteAsync));

        //log-in
        if (!await _googlePhotosSvc.LoginAsync(stoppingToken)) throw new GooglePhotosException($"login failed!");

        //get existing/create new album
        var albumTitle = $"{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}-{Guid.NewGuid()}";//make-up a random title
        var album = await _googlePhotosSvc.GetOrCreateAlbumAsync(albumTitle) ?? throw new GooglePhotosException("album creation failed!");
        _logger.LogInformation("{ClassName} {Name} '{Title}' id is '{Id}'", nameof(TestBgService), nameof(album), album.title, album.id);

        //upload single media item and assign to album
        var path = $"{_testFolder}test1.jpg";
        var mediaItem = await _googlePhotosSvc.UploadSingle(path, album.id) ?? throw new GooglePhotosException($"media item '{path}' upload failed!");
        _logger.LogInformation("{ClassName} {Name} '{FileName}' id is '{Id}'",
            nameof(TestBgService), nameof(mediaItem), mediaItem.mediaItem.filename, mediaItem.mediaItem.id);

        //retrieve all media items in the album
        var albumMediaItems = await _googlePhotosSvc.GetMediaItemsByAlbumAsync(album.id, cancellationToken: stoppingToken).ToListAsync(stoppingToken) ?? throw new GooglePhotosException("retrieve media items by album id failed!");
        var i = 1;
        foreach (var item in albumMediaItems)
        {
            _logger.LogInformation("{ClassName} album #{I} {FileName} {Width}x{Height}", nameof(TestBgService), i, item.filename,
                item.mediaMetadata.width, item.mediaMetadata.height);
            i++;
        }

        _logger.LogDebug("{ClassName} exiting {MethodName}...", nameof(TestBgService), nameof(ExecuteAsync));
        _appLifetime.StopApplication();
    }
}
