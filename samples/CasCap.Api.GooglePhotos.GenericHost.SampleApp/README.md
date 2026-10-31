# CasCap.Api.GooglePhotos.GenericHost.SampleApp

A .NET 10 Google Photos sample using `Host.CreateApplicationBuilder`, `AddGooglePhotos`, validated
options, dependency injection and a one-shot `BackgroundService`. It creates an album, uploads one
configured file, counts the album's items and stops the host.

## Prerequisites

Use a stable .NET 10 SDK and enable the Google Photos Library API with a Desktop OAuth client.
Complete the [Google Cloud setup](../../README.md#quick-start). The sample requests `AppendOnly` and
`ReadOnlyAppCreatedData`, so it can read back content created by its own OAuth client, not the entire
Google Photos library.

Release consumes published shared dependencies. Debug additionally requires an adjacent
[CasCap.Common checkout](https://github.com/f2calv/CasCap.Common).

## Configuration Examples

From the repository root, store credentials and an existing media path in User Secrets:

```powershell
dotnet user-secrets set "CasCap:GooglePhotosOptions:User" "local-user" --project .\samples\CasCap.Api.GooglePhotos.GenericHost.SampleApp
dotnet user-secrets set "CasCap:GooglePhotosOptions:ClientId" "your-client-id" --project .\samples\CasCap.Api.GooglePhotos.GenericHost.SampleApp
dotnet user-secrets set "CasCap:GooglePhotosOptions:ClientSecret" "your-client-secret" --project .\samples\CasCap.Api.GooglePhotos.GenericHost.SampleApp
dotnet user-secrets set "Sample:MediaPath" "C:\media\photo.jpg" --project .\samples\CasCap.Api.GooglePhotos.GenericHost.SampleApp
```

`User` is a local cache key and need not be an email address. Replace the placeholders privately;
do not put credentials in tracked settings or launch profiles. The sample's Development launch
profile enables the host's User Secrets provider.

The host loads JSON configuration, Development user secrets, environment variables and command-line
configuration in its standard order. Environment variables override user secrets, for example
`CasCap__GooglePhotosOptions__ClientId` or `Sample__MediaPath`.

The [package configuration examples](../../src/CasCap.Api.GooglePhotos/README.md#configuration-examples)
describe the remaining options and defaults. The sample and test project currently share a User
Secrets store, so changing credentials for either affects both.

## Run

```powershell
dotnet run --project .\samples\CasCap.Api.GooglePhotos.GenericHost.SampleApp --configuration Release
```

Initial authentication opens a browser. The worker creates remote media and an album and does not
remove them; use a dedicated account and synthetic media. Progress logs report completion and item
counts without album identifiers or titles. The worker stops the host after success or failure and sets a
nonzero exit code on failure.

## Walkthrough

[Program.cs](Program.cs) registers configuration, clients and the worker.
[GooglePhotosWorker](Services/GooglePhotosWorker.cs) reads the media path, authenticates, finds or
creates the album, uploads and reads the paged album contents.

See the [direct console SampleApp](../CasCap.Api.GooglePhotos.Console.SampleApp/README.md) for explicit HTTP construction and the
[root README](../../README.md) for Google's API access limits.
