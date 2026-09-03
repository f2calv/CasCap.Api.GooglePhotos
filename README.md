---
title: CasCap.Api.GooglePhotos
description: Unofficial .NET client library for the Google Photos Library and Picker APIs.
---

## Overview

[CasCap.Api.GooglePhotos-badge]: https://img.shields.io/nuget/v/CasCap.Api.GooglePhotos?color=blue
[CasCap.Api.GooglePhotos-url]: https://nuget.org/packages/CasCap.Api.GooglePhotos

![CI](https://github.com/f2calv/CasCap.Api.GooglePhotos/actions/workflows/ci.yml/badge.svg) [![NuGet][CasCap.Api.GooglePhotos-badge]][CasCap.Api.GooglePhotos-url]

CasCap.Api.GooglePhotos is an unofficial .NET 10 client for the Google Photos Library API and Picker API.

Google changed the Photos APIs on March 31, 2025:

* The Library API can manage only albums and media items created by your application.
* The Picker API lets a user explicitly select existing photos and videos for your application.
* Library API sharing and shared-album operations are no longer available.

Applications that previously listed a user's complete library must migrate that workflow to the Picker API. See [Google's API update](https://developers.google.com/photos/support/updates) and issue [#208](https://github.com/f2calv/CasCap.Api.GooglePhotos/issues/208).

## Migrating to v4

Version 4 is a breaking release:

* The library targets .NET 10 only.
* Public DTO properties and enum members use PascalCase while preserving Google's JSON wire names.
* Removed Google sharing APIs and scopes are no longer exposed. `ShareInfo`, `SharedAlbumOptions` and `ContributorInfo`, along with `Album.ShareInfo` and `MediaItem.ContributorInfo`, are gone because no remaining scope can populate them.
* Library API operations now apply only to content created by the configured OAuth client.
* OAuth cache entries are isolated by local user, OAuth client ID, and requested scopes.
* Authorization moved to the shared `GooglePhotosCredentialProvider` and is applied per request. The `LoginAsync` overloads that took OAuth settings or a `GooglePhotosOptions` instance are removed; configure options through `AddGooglePhotos` and call `LoginAsync(cancellationToken)`.

Applications that need existing user media must use `GooglePhotosPickerService`. Recompile consumers and update renamed DTO members before upgrading.

## Installation

```powershell
dotnet package add CasCap.Api.GooglePhotos
dotnet package add Microsoft.Extensions.Hosting
```

## Quick Start

### Configure Google Cloud

1. Create or select a project in the [Google Cloud Console](https://console.cloud.google.com/).
2. Open **APIs & Services > Library**, search for "Photos", and enable **Google Photos Library API** if the application creates or manages app-created media and albums.
3. Enable **Google Photos Picker API** if the application lets users select existing media. Do not enable **Google Picker API** by mistake; it is a different product.
4. Open **Google Auth Platform**, configure the consent screen, select an audience, and declare the required Photos scopes under **Data Access**.
5. If the app has an external audience and remains in testing, add each Google account that will run it as a test user.
6. Open **APIs & Services > Credentials**, create an **OAuth client ID**, and choose **Desktop app**. This library uses Google's installed-application loopback flow.
7. Record the client ID and client secret in a secret store. Never place them in source control or tracked configuration.

The Google Photos APIs require an authenticated Google user and do not support service accounts. Public applications must also complete Google's OAuth verification process. Keep the OAuth client ID stable because Google associates API-created resources with the client that created them.

### Configure the application

Register the clients through standard .NET configuration and dependency injection:

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddGooglePhotos(builder.Configuration);

await builder.Build().RunAsync();
```

For local development, initialize User Secrets in the consuming application project and store credentials there:

```powershell
$env:DOTNET_ENVIRONMENT = "Development"
dotnet user-secrets init
dotnet user-secrets set "CasCap:GooglePhotosOptions:User" "local-user"
dotnet user-secrets set "CasCap:GooglePhotosOptions:ClientId" "your-client-id"
dotnet user-secrets set "CasCap:GooglePhotosOptions:ClientSecret" "your-client-secret"
```

`User` is a local token-cache key. It identifies the cached grant on this machine and does not need to be the Google account's email address.

The first call to `LoginAsync` opens the system browser for consent. Cached grants are separated by user and requested scope set. If a user declines a required scope, revoke or remove that cached grant and authenticate again. For an external consent screen in testing, Google can expire refresh tokens after seven days, so repeated authentication during development is expected.

`AddGooglePhotos` registers a singleton `GooglePhotosCredentialProvider`. Authorizing through either client stores the grant there, so one `LoginAsync` call covers the Library API client, the Picker API client, and every later resolution of them. A delegating handler reads the provider on each request, so an expiring access token is refreshed automatically rather than being captured once at login.

## OAuth Scopes

| Enum value                       | Google scope                                       | Capability                                        |
| -------------------------------- | -------------------------------------------------- | ------------------------------------------------- |
| `AppendOnly`                     | `photoslibrary.appendonly`                         | Create media, albums, and enrichments             |
| `ReadOnlyAppCreatedData`         | `photoslibrary.readonly.appcreateddata`            | Read media and albums created by the application  |
| `EditAppCreatedData`             | `photoslibrary.edit.appcreateddata`                | Edit and organize application-created content     |
| `PickerMediaItemsReadOnly`       | `photospicker.mediaitems.readonly`                 | Create Picker sessions and read selected media    |

Request only the scopes needed by the application. The library creates a separate cached grant when the requested scope set changes.

## Configuration

Tracked settings must contain placeholders only. Store credentials with .NET User Secrets locally and environment variables or secret-backed providers in deployed environments. A complete configuration that enables both APIs is shown below; remove scopes your application does not use.

```json
{
  "CasCap": {
    "GooglePhotosOptions": {
      "User": null,
      "Scopes": [
        "AppendOnly",
        "ReadOnlyAppCreatedData",
        "EditAppCreatedData",
        "PickerMediaItemsReadOnly"
      ],
      "FileDataStoreFullPathOverride": null,
      "WriteRateLimit": {
        "Enabled": false,
        "PermitLimit": 8,
        "QueueLimit": 100,
        "SegmentsPerWindow": 6,
        "WindowSeconds": 60
      },
      "RequestTimeoutSeconds": 90,
      "UploadTimeoutSeconds": 3600,
      "ClientId": null,
      "ClientSecret": null
    }
  }
}
```

Environment variables use the standard double-underscore form, for example `CasCap__GooglePhotosOptions__ClientId`.

`RequestTimeoutSeconds` bounds a single Library or Picker API request. `UploadTimeoutSeconds` bounds a single media upload request, which streams whole files or large chunks and can legitimately run far longer; raise it when uploading large videos over a slow link.

`WriteRateLimit` optionally queues mutating Library API requests through an oldest-first sliding window. It is disabled by default because Google quota values can vary. Configure the limits for the quota assigned to your project. The limiter is local to one process and does not coordinate multiple application instances. Reads and Picker API requests bypass it.

## Library API

`GooglePhotosService` uploads and manages content created by the application. List, get, and search operations do not expose unrelated existing content from the user's library.

For uploads, enable the Library API and request `AppendOnly`. Add `ReadOnlyAppCreatedData` when retrieving app-created content, including the get-or-create example below, and `EditAppCreatedData` when organizing it. Google performs an upload in two steps: upload bytes to obtain a token, then create the media item. `UploadSingle` handles both steps.

```csharp
using CasCap.Services;

public sealed class PhotoImportService(GooglePhotosService googlePhotosSvc)
{
    public async Task UploadAsync(string path, CancellationToken cancellationToken)
    {
        if (!await googlePhotosSvc.LoginAsync(cancellationToken))
            throw new InvalidOperationException("Google Photos authentication failed.");

        var album = await googlePhotosSvc.GetOrCreateAlbumAsync(
            "Application uploads",
            cancellationToken: cancellationToken);
        if (album is null)
            throw new InvalidOperationException("Album creation failed.");

        await googlePhotosSvc.UploadSingle(
            path,
            album.Id,
            cancellationToken: cancellationToken);
    }
}
```

Uploads are streamed. Resumable multipart uploads use bounded pooled buffers rather than loading complete media files into memory.

## Picker API

`GooglePhotosPickerService` provides the user-mediated flow for existing photos and videos:

1. Authenticate and create a picking session.
2. Present `PickerUri` to the user outside an iframe and in a browser signed into the Google Account that owns the session.
3. Poll `GetSessionAsync` using Google's returned polling configuration.
4. List selected media after `MediaItemsSet` becomes `true`.
5. Stream selected media bytes and delete the session when finished.

Enable the Google Photos Picker API and request only `PickerMediaItemsReadOnly` for a Picker-only application. Open `PickerUri` in a browser or native browser surface signed into the same Google Account, never in an iframe. Respect `PollingConfig.PollInterval` and `PollingConfig.TimeoutIn` when polling, and always delete the session after downloading the selected media.

```csharp
using CasCap.Models.Picker;
using CasCap.Services;

public sealed class PhotoPickerService(GooglePhotosPickerService pickerSvc)
{
    public async Task<PickingSession> StartAsync(CancellationToken cancellationToken)
    {
        if (!await pickerSvc.LoginAsync(cancellationToken))
            throw new InvalidOperationException("Google Photos authentication failed.");

        return await pickerSvc.CreateSessionAsync(
            maxItemCount: 25,
            cancellationToken: cancellationToken);
    }

    public IAsyncEnumerable<PickedMediaItem> GetSelectedAsync(
        string sessionId,
        CancellationToken cancellationToken)
        => pickerSvc.GetMediaItemsAsync(sessionId, cancellationToken: cancellationToken);

    public Task DeleteSessionAsync(
      string sessionId,
      CancellationToken cancellationToken)
      => pickerSvc.DeleteSessionAsync(sessionId, cancellationToken);
}
```

Picked-media base URLs expire and downloads require the OAuth bearer token. Use `DownloadPhotoAsync` or `DownloadVideoAsync` to stream bytes through the authenticated client.

## Samples and Tests

* [ConsoleApp](samples/ConsoleApp) demonstrates direct Library API construction without dependency injection.
* [GenericHost](samples/GenericHost) demonstrates configuration, logging, and dependency injection.
* [Integration tests](src/CasCap.Api.GooglePhotos.Tests) cover authentication and API behavior against a dedicated test account.

Integration tests require credentials and can create albums or upload media. Review the test README and obtain explicit approval before running them.

## Dependencies

| Package                                | Purpose                                           |
| -------------------------------------- | ------------------------------------------------- |
| `Google.Apis.Auth`                     | OAuth authentication and cached token grants      |
| `Microsoft.Extensions.Http.Resilience` | HTTP timeouts, circuit breaking, and safe retries |
| `Microsoft.AspNetCore.WebUtilities`    | Query-string construction                         |
| `System.Threading.RateLimiting`        | Optional temporal Library API write limiting      |
| `MimeTypeMapOfficial`                  | Upload MIME-type detection                        |
| `CasCap.Common.Net`                    | Shared HTTP and serialization infrastructure      |

## Resources

* [Google Photos API overview](https://developers.google.com/photos/overview/about)
* [Library API reference](https://developers.google.com/photos/library/reference/rest)
* [Picker API guide](https://developers.google.com/photos/picker/guides/get-started-picker)
* [Authorization scopes](https://developers.google.com/photos/overview/authorization)
* [Issue tracker](https://github.com/f2calv/CasCap.Api.GooglePhotos/issues)

## License

CasCap.Api.GooglePhotos is provided under the [MIT license](LICENSE).
