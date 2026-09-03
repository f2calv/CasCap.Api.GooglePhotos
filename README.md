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

## Installation

```powershell
dotnet add package CasCap.Api.GooglePhotos
```

## Google Cloud Setup

1. Create or select a project in the [Google Cloud Console](https://console.cloud.google.com/).
2. Enable the Google Photos Library API and Google Photos Picker API.
3. Configure the OAuth consent screen.
4. Create an OAuth client for the application type you are building.
5. Store the client ID, client secret, and Google account identifier outside source control.

The Google Photos APIs require user OAuth 2.0 authorization and do not support service accounts.

## OAuth Scopes

| Enum value                       | Google scope                                       | Capability                                        |
| -------------------------------- | -------------------------------------------------- | ------------------------------------------------- |
| `AppendOnly`                     | `photoslibrary.appendonly`                         | Create media, albums, and enrichments             |
| `ReadOnlyAppCreatedData`         | `photoslibrary.readonly.appcreateddata`            | Read media and albums created by the application  |
| `EditAppCreatedData`             | `photoslibrary.edit.appcreateddata`                | Edit and organize application-created content     |
| `PickerMediaItemsReadOnly`       | `photospicker.mediaitems.readonly`                 | Create Picker sessions and read selected media    |

Request only the scopes needed by the application. If scopes change for an existing cached user grant, delete the cached OAuth response and authenticate again.

## Configuration

Tracked settings must contain placeholders only. Store credentials with .NET User Secrets locally and environment variables or secret-backed providers in deployed environments.

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
      "ClientId": null,
      "ClientSecret": null
    }
  }
}
```

```powershell
dotnet user-secrets set "CasCap:GooglePhotosOptions:User" "user@example.com"
dotnet user-secrets set "CasCap:GooglePhotosOptions:ClientId" "your-client-id"
dotnet user-secrets set "CasCap:GooglePhotosOptions:ClientSecret" "your-client-secret"
```

Environment variables use the standard double-underscore form, for example `CasCap__GooglePhotosOptions__ClientId`.

## Dependency Injection

`AddGooglePhotos` registers validated options plus typed clients for both APIs.

```csharp
var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddGooglePhotos(builder.Configuration);

await builder.Build().RunAsync();
```

## Library API

`GooglePhotosService` uploads and manages content created by the application. List, get, and search operations do not expose unrelated existing content from the user's library.

```csharp
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
            album.id,
            cancellationToken: cancellationToken);
    }
}
```

Uploads are streamed. Resumable multipart uploads use bounded pooled buffers rather than loading complete media files into memory.

## Picker API

`GooglePhotosPickerService` provides the user-mediated flow for existing photos and videos:

1. Authenticate and create a picking session.
2. Present `PickerUri` to the user outside an iframe.
3. Poll `GetSessionAsync` using Google's returned polling configuration.
4. List selected media after `MediaItemsSet` becomes `true`.
5. Stream selected media bytes and delete the session when finished.

```csharp
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
}
```

Picked-media base URLs expire and downloads require the OAuth bearer token. Use `DownloadPhotoAsync` or `DownloadVideoAsync` to stream bytes through the authenticated client.

## Samples and Tests

* [ConsoleApp](samples/ConsoleApp) demonstrates direct Library API construction without dependency injection.
* [GenericHost](samples/GenericHost) demonstrates configuration, logging, and dependency injection.
* [Integration tests](src/CasCap.Api.GooglePhotos.Tests) cover authentication and API behavior against a dedicated test account.

Integration tests require credentials and can create albums or upload media. Review the test README and obtain explicit approval before running them.

## Dependencies

| Package                                | Purpose                                          |
| -------------------------------------- | ------------------------------------------------ |
| `Google.Apis.Auth`                     | OAuth authentication and cached token grants     |
| `Microsoft.Extensions.Http.Resilience` | HTTP timeouts, rate limiting, and safe retries   |
| `Microsoft.AspNetCore.WebUtilities`    | Query-string construction                        |
| `MimeTypeMapOfficial`                  | Upload MIME-type detection                       |
| `CasCap.Common.Net`                    | Shared HTTP and serialization infrastructure     |

## Resources

* [Google Photos API overview](https://developers.google.com/photos/overview/about)
* [Library API reference](https://developers.google.com/photos/library/reference/rest)
* [Picker API guide](https://developers.google.com/photos/picker/guides/get-started-picker)
* [Authorization scopes](https://developers.google.com/photos/overview/authorization)
* [Issue tracker](https://github.com/f2calv/CasCap.Api.GooglePhotos/issues)

## License

CasCap.Api.GooglePhotos is provided under the [MIT license](LICENSE).
