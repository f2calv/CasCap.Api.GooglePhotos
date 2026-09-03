namespace CasCap.Services;

/// <summary>Uploads one configured media file into a new album and reports the album contents.</summary>
public sealed class GooglePhotosWorker(
    ILogger<GooglePhotosWorker> logger,
    IConfiguration configuration,
    IHostApplicationLifetime appLifetime,
    GooglePhotosService googlePhotosSvc) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            var mediaPath = configuration["Sample:MediaPath"];
            ArgumentException.ThrowIfNullOrWhiteSpace(mediaPath);
            if (!File.Exists(mediaPath))
                throw new FileNotFoundException("The configured sample media file was not found.", mediaPath);

            if (!await googlePhotosSvc.LoginAsync(stoppingToken))
                throw new GooglePhotosException("Google Photos login failed.");

            var albumTitle = $"sample-{DateTime.UtcNow:yyyyMMdd-HHmmss}";
            var album = await googlePhotosSvc.GetOrCreateAlbumAsync(albumTitle, cancellationToken: stoppingToken)
                ?? throw new GooglePhotosException("Album creation failed.");
            logger.LogInformation("{ClassName} created album", nameof(GooglePhotosWorker));

            _ = await googlePhotosSvc.UploadSingle(mediaPath, album.Id, cancellationToken: stoppingToken)
                ?? throw new GooglePhotosException("Media item upload failed.");

            var albumMediaItems = await googlePhotosSvc.GetMediaItemsByAlbumAsync(album.Id, cancellationToken: stoppingToken).ToListAsync(stoppingToken);
            logger.LogInformation("{ClassName} retrieved {MediaItemCount} media items", nameof(GooglePhotosWorker), albumMediaItems.Count);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
        catch
        {
            Environment.ExitCode = 1;
            throw;
        }
        finally
        {
            appLifetime.StopApplication();
        }

    }
}
