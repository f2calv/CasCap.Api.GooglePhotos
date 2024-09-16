using CasCap.Exceptions;

namespace CasCap.Services;

public class MyBackgroundService : BackgroundService
{
    readonly ILogger _logger;
    readonly IHostApplicationLifetime _appLifetime;
    readonly GooglePhotosService _googlePhotosSvc;

    const string _testFolder = "c:/temp/GooglePhotos/";//local folder of test media files

    public MyBackgroundService(ILogger<MyBackgroundService> logger, IHostApplicationLifetime appLifetime,
        GooglePhotosService googlePhotosSvc)
    {
        _logger = logger;
        _appLifetime = appLifetime;
        _googlePhotosSvc = googlePhotosSvc;
    }

    protected async override Task ExecuteAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("{serviceName} starting {methodName}...", nameof(MyBackgroundService), nameof(ExecuteAsync));

        //log-in
        if (!await _googlePhotosSvc.LoginAsync(cancellationToken)) throw new GooglePhotosException($"login failed!");

        //get existing/create new album
        var albumTitle = $"{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}-{Guid.NewGuid()}";//make-up a random title
        var album = await _googlePhotosSvc.GetOrCreateAlbumAsync(albumTitle) ?? throw new GooglePhotosException("album creation failed!");
        _logger.LogInformation("{serviceName} {name} '{title}' id is '{id}'", nameof(MyBackgroundService), nameof(album), album.title, album.id);

        //upload single media item and assign to album
        var path = $"{_testFolder}test1.jpg";
        var mediaItem = await _googlePhotosSvc.UploadSingle(path, album.id) ?? throw new GooglePhotosException($"media item '{path}' upload failed!");
        _logger.LogInformation("{serviceName} {name} '{filename}' id is '{id}'",
            nameof(MyBackgroundService), nameof(mediaItem), mediaItem.mediaItem.filename, mediaItem.mediaItem.id);

        //retrieve all media items in the album
        var albumMediaItems = await _googlePhotosSvc.GetMediaItemsByAlbumAsync(album.id, cancellationToken: cancellationToken).ToListAsync(cancellationToken) ?? throw new GooglePhotosException("retrieve media items by album id failed!");
        var i = 1;
        foreach (var item in albumMediaItems)
        {
            _logger.LogInformation("{serviceName} album #{i} {filename} {width}x{height}", nameof(MyBackgroundService), i, item.filename,
                item.mediaMetadata.width, item.mediaMetadata.height);
            i++;
        }

        _logger.LogInformation("{serviceName} exiting {methodName}...", nameof(MyBackgroundService), nameof(ExecuteAsync));
        _appLifetime.StopApplication();
    }
}
