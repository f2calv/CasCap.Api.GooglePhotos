# ConsoleApp

A .NET 10 console sample that constructs `GooglePhotosService`, HTTP handlers, logging and options
directly. It creates an album, uploads one media file and lists that album's contents.

Because it bypasses `AddGooglePhotos`, it also bypasses the resilience pipeline and optional write
rate limiter. See the [GenericHost sample](../GenericHost/README.md) for the dependency-injection path.

## Prerequisites

Use a stable .NET 10 SDK and enable the Google Photos Library API with a Desktop OAuth client.
Follow the [Google Cloud setup guide](../../README.md#quick-start), including consent and test-user
requirements. The sample requests `AppendOnly`, `ReadOnlyAppCreatedData` and `EditAppCreatedData`.
It can read only content created by its OAuth client, not the user's whole library.

Release configuration uses published shared dependencies. Debug additionally requires an adjacent
[CasCap.Common checkout](https://github.com/f2calv/CasCap.Common).

## Configuration

The sample reads these environment variables directly, not the `CasCap:GooglePhotosOptions` section:

```powershell
$env:GOOGLE_PHOTOS_USER = "local-user"
$env:GOOGLE_PHOTOS_CLIENT_ID = "your-client-id"
$env:GOOGLE_PHOTOS_CLIENT_SECRET = "your-client-secret"
```

`GOOGLE_PHOTOS_USER` is a local token-cache key, not necessarily an email address. Supply your own
OAuth values privately. Leave tracked launch-profile credential placeholders empty.

## Run

From the repository root, pass exactly one existing media-file path:

```powershell
dotnet run --project .\samples\ConsoleApp --configuration Release --no-launch-profile -- "C:\media\photo.jpg"
```

`--no-launch-profile` is important: the checked-in launch profile contains blank environment-variable
placeholders that would otherwise override the values set above.

Initial authentication opens the browser; subsequent runs reuse the cached grant where usable.
The sample creates remote media and an album and does not remove them. Use a dedicated account and
synthetic media. Output includes album/media identifiers, filenames and the local cache directory;
do not publish raw output containing private information.

## Walkthrough

[Program.cs](Program.cs) demonstrates argument validation and Ctrl+C cancellation, logger/options
construction, an OAuth credential provider and authorization handler, then login, album creation,
upload and paged album enumeration. The hand-built client owns its HTTP resources with `using`.

See the [package README](../../src/CasCap.Api.GooglePhotos/README.md) for supported APIs, token-cache
behavior and current Google service restrictions.
