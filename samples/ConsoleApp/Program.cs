if (args.Length != 1)
{
    Console.Error.WriteLine("Usage: dotnet run --project samples/ConsoleApp -- <media-file>");
    return 1;
}

var mediaPath = Path.GetFullPath(args[0]);
if (!File.Exists(mediaPath))
{
    Console.Error.WriteLine($"Cannot find media file '{mediaPath}'.");
    return 1;
}

using var cancellationTokenSource = new CancellationTokenSource();
Console.CancelKeyPress += (_, eventArgs) =>
{
    if (!cancellationTokenSource.IsCancellationRequested)
    {
        eventArgs.Cancel = true;
        cancellationTokenSource.Cancel();
    }
};

using var loggerFactory = LoggerFactory.Create(builder => builder.AddSimpleConsole());
var logger = loggerFactory.CreateLogger<GooglePhotosService>();

var options = new GooglePhotosOptions
{
    User = GetRequiredEnvironmentVariable("GOOGLE_PHOTOS_USER"),
    ClientId = GetRequiredEnvironmentVariable("GOOGLE_PHOTOS_CLIENT_ID"),
    ClientSecret = GetRequiredEnvironmentVariable("GOOGLE_PHOTOS_CLIENT_SECRET"),
    Scopes =
    [
        GooglePhotosScope.AppendOnly,
        GooglePhotosScope.ReadOnlyAppCreatedData,
        GooglePhotosScope.EditAppCreatedData
    ]
};

using var handler = new HttpClientHandler
{
    AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
};

//Without dependency injection the credential provider and its handler have to be wired up by hand.
var credentialProvider = new GooglePhotosCredentialProvider(
    loggerFactory.CreateLogger<GooglePhotosCredentialProvider>(),
    Options.Create(options));
using var authorizationHandler = credentialProvider.CreateAuthorizationHandler(handler);
using var client = new HttpClient(authorizationHandler) { BaseAddress = new Uri(options.BaseAddress) };

var googlePhotosSvc = new GooglePhotosService(logger, Options.Create(options), credentialProvider, client);
var cancellationToken = cancellationTokenSource.Token;

if (!await googlePhotosSvc.LoginAsync(cancellationToken))
    throw new GooglePhotosException("Google Photos login failed.");

var albumTitle = $"sample-{DateTime.UtcNow:yyyyMMdd-HHmmss}";
var album = await googlePhotosSvc.GetOrCreateAlbumAsync(albumTitle, cancellationToken: cancellationToken)
    ?? throw new GooglePhotosException("Album creation failed.");

Console.WriteLine($"{nameof(album)} '{album.Title}' id is '{album.Id}'");

var mediaItem = await googlePhotosSvc.UploadSingle(mediaPath, album.Id, cancellationToken: cancellationToken)
    ?? throw new GooglePhotosException("Media item upload failed.");

Console.WriteLine($"{nameof(mediaItem)} '{mediaItem.MediaItem.Filename}' id is '{mediaItem.MediaItem.Id}'");

var itemCount = 0;
await foreach (var item in googlePhotosSvc.GetMediaItemsByAlbumAsync(album.Id, cancellationToken: cancellationToken))
{
    itemCount++;
    Console.WriteLine($"{itemCount}\t{item.Filename}\t{item.MediaMetadata.Width}x{item.MediaMetadata.Height}");
}

return itemCount > 0 ? 0 : 1;

static string GetRequiredEnvironmentVariable(string name)
    => Environment.GetEnvironmentVariable(name)
        ?? throw new InvalidOperationException($"Set the {name} environment variable before running the sample.");
