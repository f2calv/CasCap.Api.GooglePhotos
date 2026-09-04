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
            //1) The media file to upload comes from configuration, so it can be set in appsettings.json,
            //   User Secrets or an environment variable without touching the code.
            var mediaPath = configuration["Sample:MediaPath"];
            ArgumentException.ThrowIfNullOrWhiteSpace(mediaPath);
            if (!File.Exists(mediaPath))
                throw new FileNotFoundException("The configured sample media file was not found.", mediaPath);

            //2) Authenticate once. The grant is held by the shared GooglePhotosCredentialProvider, so it
            //   also covers the Picker client and any later resolution of either client.
            //   The first run opens the system browser for consent.
            if (!await googlePhotosSvc.LoginAsync(stoppingToken))
                throw new GooglePhotosException("Google Photos login failed.");

            //3) Find or create an album. This only sees albums created by this OAuth client.
            var albumTitle = $"sample-{DateTime.UtcNow:yyyyMMdd-HHmmss}";
            var album = await googlePhotosSvc.GetOrCreateAlbumAsync(albumTitle, cancellationToken: stoppingToken)
                ?? throw new GooglePhotosException("Album creation failed.");
            logger.LogInformation("{ClassName} created album", nameof(GooglePhotosWorker));

            //4) Upload the bytes and create the media item in the album, in one call.
            _ = await googlePhotosSvc.UploadSingleAsync(mediaPath, album.Id, cancellationToken: stoppingToken)
                ?? throw new GooglePhotosException("Media item upload failed.");

            //5) Read the album back. Paging is handled by the returned IAsyncEnumerable.
            var albumMediaItems = await googlePhotosSvc.GetMediaItemsByAlbumAsync(album.Id, cancellationToken: stoppingToken).ToListAsync(stoppingToken);
            logger.LogInformation("{ClassName} retrieved {MediaItemCount} media items", nameof(GooglePhotosWorker), albumMediaItems.Count);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            //Shutdown was requested, which is not a failure.
        }
        catch
        {
            Environment.ExitCode = 1;
            throw;
        }
        finally
        {
            //This sample is a one-shot job rather than a long-running service, so stop the host when done.
            appLifetime.StopApplication();
        }
    }
}
