//This sample wires the library up by hand, without dependency injection, so that each moving part is
//visible. See the GenericHost sample for the shorter, more typical AddGooglePhotos registration.

//1) Read the media file to upload from the command line.
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

//2) Cancel cleanly on Ctrl+C. Every library call takes a CancellationToken, including the uploads.
using var cancellationTokenSource = new CancellationTokenSource();
Console.CancelKeyPress += (_, eventArgs) =>
{
    if (!cancellationTokenSource.IsCancellationRequested)
    {
        eventArgs.Cancel = true;
        cancellationTokenSource.Cancel();
    }
};
var cancellationToken = cancellationTokenSource.Token;

//3) The library only ever depends on ILogger<T>, so any logging provider will do.
using var loggerFactory = LoggerFactory.Create(builder => builder.AddSimpleConsole());

//4) Build the options by hand. Credentials come from environment variables here; set them in
//   Properties/launchSettings.json when debugging. The GenericHost sample uses User Secrets instead.
//   Request only the scopes the application needs: AppendOnly to upload, ReadOnlyAppCreatedData to read
//   back what this OAuth client created, and EditAppCreatedData to organise it.
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

//5) Google's auth library caches the OAuth grant on disk. Knowing where it lives explains why the browser
//   only opens on the first run, and where to clear a stale grant when the requested scopes change.
var tokenCacheFolder = options.FileDataStoreFullPathOverride ?? GooglePhotosOptions.FileDataStoreFullPathDefault;
var cachedGrantCount = Directory.Exists(tokenCacheFolder) ? Directory.GetFiles(tokenCacheFolder).Length : 0;
Console.WriteLine($"OAuth token cache: {tokenCacheFolder}");
Console.WriteLine(cachedGrantCount == 0
    ? "  no cached grant yet, so a browser will open for consent"
    : $"  {cachedGrantCount} cached file(s), so consent should be skipped");

//6) One HttpClient is created and reused for every call. AddGooglePhotos normally builds this handler
//   chain; by hand it is authorization handler -> decompression handler -> network.
using var handler = new HttpClientHandler
{
    AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
};

//7) The credential provider holds the OAuth grant and refreshes the access token. The handler it creates
//   applies that token per request, so a long-running process never sends an expired one.
var credentialProvider = new GooglePhotosCredentialProvider(
    loggerFactory.CreateLogger<GooglePhotosCredentialProvider>(),
    Options.Create(options));
using var authorizationHandler = credentialProvider.CreateAuthorizationHandler(handler);
using var client = new HttpClient(authorizationHandler) { BaseAddress = new Uri(options.BaseAddress) };

//8) Construct the service from the pieces above, in lieu of dependency injection. Note that this
//   hand-built client has no resilience pipeline and no write rate limiting.
var googlePhotosSvc = new GooglePhotosService(
    loggerFactory.CreateLogger<GooglePhotosService>(),
    Options.Create(options),
    credentialProvider,
    client);

//9) Authenticate. The first run opens the system browser; later runs reuse the cached grant from step 5.
if (!await googlePhotosSvc.LoginAsync(cancellationToken))
    throw new GooglePhotosException("Google Photos login failed.");

//10) Find or create an album. Since the March 2025 API change this only sees albums that this OAuth
//    client created, never the rest of the user's library.
var albumTitle = $"sample-{DateTime.UtcNow:yyyyMMdd-HHmmss}";
var album = await googlePhotosSvc.GetOrCreateAlbumAsync(albumTitle, cancellationToken: cancellationToken)
    ?? throw new GooglePhotosException("Album creation failed.");

Console.WriteLine($"{nameof(album)} '{album.Title}' id is '{album.Id}'");

//11) Upload. Google splits this into two steps, uploading the bytes for a token and then creating the
//    media item from that token; UploadSingleAsync does both.
var mediaItem = await googlePhotosSvc.UploadSingleAsync(mediaPath, album.Id, cancellationToken: cancellationToken)
    ?? throw new GooglePhotosException("Media item upload failed.");

Console.WriteLine($"{nameof(mediaItem)} '{mediaItem.MediaItem.Filename}' id is '{mediaItem.MediaItem.Id}'");

//12) Read the album back. Results stream as an IAsyncEnumerable, so paging is handled for you.
var itemCount = 0;
await foreach (var item in googlePhotosSvc.GetMediaItemsByAlbumAsync(album.Id, cancellationToken: cancellationToken))
{
    itemCount++;
    Console.WriteLine($"{itemCount}\t{item.Filename}\t{item.MediaMetadata.Width}x{item.MediaMetadata.Height}");
}

return itemCount > 0 ? 0 : 1;

//Treats an empty value as missing, so the blank placeholders in launchSettings.json fail clearly.
static string GetRequiredEnvironmentVariable(string name)
{
    var value = Environment.GetEnvironmentVariable(name);
    return string.IsNullOrWhiteSpace(value)
        ? throw new InvalidOperationException($"Set the {name} environment variable before running the sample.")
        : value;
}
