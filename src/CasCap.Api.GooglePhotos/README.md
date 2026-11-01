# CasCap.Api.GooglePhotos

An unofficial .NET 10 client for the Google Photos Library API and Picker API. It provides typed
models, OAuth authentication, paging, streaming uploads and optional client-side write rate limiting.

[![NuGet](https://img.shields.io/nuget/v/CasCap.Api.GooglePhotos?color=blue)](https://nuget.org/packages/CasCap.Api.GooglePhotos)

## Google API Access Limits

Since 31 March 2025, the Library API only manages albums and media items created by your application.
It cannot enumerate or back up a user's whole library, and Google withdrew Library API sharing.
Use the Picker API when a user must interactively choose existing photos and videos, one session at
a time. These are Google service restrictions, not limitations that a different client can bypass.
Verified against [Google's API update](https://developers.google.com/photos/support/updates) on
**20 October 2026**.

## Installation

Targets `net10.0` only.

```powershell
dotnet package add CasCap.Api.GooglePhotos
dotnet package add Microsoft.Extensions.Hosting
```

## Quick Start

Enable the relevant Google Photos API in Google Cloud, configure the OAuth consent screen and
create a **Desktop app** OAuth client. Photos requires user authorization; service accounts are not
supported. `LoginAsync` uses the installed-app browser/loopback flow, so complete initial consent
where the browser and callback are available. Configure test users and OAuth verification as
required by Google's policies.

After supplying the configuration below through a secret-backed provider:

```csharp
using CasCap.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddGooglePhotos(builder.Configuration);
using var host = builder.Build();

var photos = host.Services.GetRequiredService<GooglePhotosService>();
if (!await photos.LoginAsync())
    throw new InvalidOperationException("Google Photos authentication failed.");

var album = await photos.GetOrCreateAlbumAsync("Application uploads")
    ?? throw new InvalidOperationException("Album creation failed.");
var result = await photos.UploadSingleAsync(mediaPath, album.Id)
    ?? throw new InvalidOperationException("Media upload failed.");
```

`mediaPath` must name an existing supported media file. This example creates remote content and
requires `AppendOnly` plus `ReadOnlyAppCreatedData`. Uploads transfer bytes for an upload token, then
create the media item; `UploadSingleAsync` combines both stages. Inspect returned creation results
for API-level status rather than assuming every item in a batch succeeded.

## Services

| Type | Purpose |
| --- | --- |
| [`GooglePhotosService`](https://github.com/f2calv/CasCap.Api.GooglePhotos/blob/ec3cee350804e73cd48461c1a2df063ad14c686e/src/CasCap.Api.GooglePhotos/Services/GooglePhotosService.cs) | Library API client: albums, enrichments, media item search, uploads and downloads for application-created content |
| [`GooglePhotosPickerService`](https://github.com/f2calv/CasCap.Api.GooglePhotos/blob/ec3cee350804e73cd48461c1a2df063ad14c686e/src/CasCap.Api.GooglePhotos/Services/GooglePhotosPickerService.cs) | Picker API client: picking sessions, selected media listing and authenticated media streaming |
| [`GooglePhotosCredentialProvider`](https://github.com/f2calv/CasCap.Api.GooglePhotos/blob/ec3cee350804e73cd48461c1a2df063ad14c686e/src/CasCap.Api.GooglePhotos/Services/GooglePhotosCredentialProvider.cs) | Singleton holding the OAuth grant shared by both clients; applied and refreshed per request |
| [`GooglePhotosOptions`](https://github.com/f2calv/CasCap.Api.GooglePhotos/blob/ec3cee350804e73cd48461c1a2df063ad14c686e/src/CasCap.Api.GooglePhotos/Models/GooglePhotosOptions.cs) | Endpoints, OAuth credentials, scopes, timeouts and write rate-limit configuration |
| [`GooglePhotosException`](https://github.com/f2calv/CasCap.Api.GooglePhotos/blob/ec3cee350804e73cd48461c1a2df063ad14c686e/src/CasCap.Api.GooglePhotos/Exceptions/GooglePhotosException.cs) | Wraps errors returned by either API |

Call `LoginAsync` once on either client. The grant is stored in `GooglePhotosCredentialProvider`, so it applies to every resolved client and its access token is refreshed automatically.

## Registration

`AddGooglePhotos` registers both clients, their `HttpClient` configuration, the standard resilience pipeline and the optional write rate limiter.

Overloads accept an `IConfiguration` section, a `GooglePhotosOptions` instance, or an `Action<GooglePhotosOptions>` delegate. All three validate the resulting options.

The configuration overload accepts the full `IConfiguration` and an optional section name, defaulting
to `CasCap:GooglePhotosOptions`. The options-instance overload copies options, including scopes and
rate-limit settings, rather than retaining their mutable collections.

## Configuration Examples

Options bind from the `CasCap:GooglePhotosOptions` configuration section. Environment variables use the standard double-underscore form, for example `CasCap__GooglePhotosOptions__ClientId`.

Minimal configuration:

```json
{
  "CasCap": {
    "GooglePhotosOptions": {
      "User": "local-user",
      "Scopes": ["AppendOnly", "ReadOnlyAppCreatedData"],
      "ClientId": "your-client-id",
      "ClientSecret": "your-client-secret"
    }
  }
}
```

Complete configuration:

```json
{
  "CasCap": {
    "GooglePhotosOptions": {
      "BaseAddress": "https://photoslibrary.googleapis.com/v1/",
      "PickerBaseAddress": "https://photospicker.googleapis.com/v1/",
      "User": "local-user",
      "Scopes": [
        "AppendOnly",
        "ReadOnlyAppCreatedData",
        "EditAppCreatedData",
        "PickerMediaItemsReadOnly"
      ],
      "FileDataStoreFullPathOverride": null,
      "RequestTimeoutSeconds": 90,
      "UploadTimeoutSeconds": 3600,
      "UploadRetryLimit": 10,
      "WriteRateLimit": {
        "Enabled": false,
        "PermitLimit": 8,
        "QueueLimit": 100,
        "SegmentsPerWindow": 6,
        "WindowSeconds": 60
      },
      "ClientId": "your-client-id",
      "ClientSecret": "your-client-secret"
    }
  }
}
```

| Key | Purpose |
| --- | --- |
| `BaseAddress` | Library API endpoint |
| `PickerBaseAddress` | Picker API endpoint |
| `User` | Local token-cache key, not necessarily the Google account email address |
| `Scopes` | OAuth scopes to request; request only what the application needs |
| `ClientId`, `ClientSecret` | Desktop app OAuth client credentials |
| `FileDataStoreFullPathOverride` | Optional directory for the cached OAuth grant |
| `RequestTimeoutSeconds` | Per-attempt timeout for non-upload API requests |
| `UploadTimeoutSeconds` | Timeout for each upload HTTP request, not a whole multipart file |
| `UploadRetryLimit` | Attempt limit per resumable multipart chunk |
| `WriteRateLimit` | Optional client-side sliding-window limiter for mutating Library API requests |

The examples contain placeholders; supply your own nonempty `User`, `ClientId` and `ClientSecret`.
`User` is a local cache key and need not be an email address. Keep credentials in .NET User Secrets
or a secret-backed provider, and protect the cached OAuth grant outside the repository.

## Authentication and Request Behavior

Cached grants are isolated by local user, OAuth client ID and requested scopes. By default Google's
authentication library stores them under the user's application-data directory in `Google.Apis.Auth`;
`FileDataStoreFullPathOverride` selects another directory. If a required scope is declined or a grant
is revoked, authenticate again after removing/revoking the unusable grant.

`SetAuth("Bearer", accessToken)` accepts an externally acquired user token shared by both clients,
but **does not refresh it**; the caller owns expiry and replacement. Automatic refresh applies to
the OAuth credential obtained through `LoginAsync`.

The DI registration adds the standard resilience pipeline, with automatic retries disabled for
unsafe HTTP methods and up to six retries for safe methods. `RequestTimeoutSeconds` is the
per-attempt API timeout; the non-upload total budget is seven times that value. Each upload HTTP
request uses `UploadTimeoutSeconds` for both its attempt and total handler budget. Resumable
multipart chunk recovery uses `UploadRetryLimit`, rather than blindly replaying mutating requests.

The optional oldest-first sliding-window write limiter is disabled by default. Its limits belong to
the handler pipeline, not Google's authoritative quota, and are not coordinated across separate
containers or application instances. Reads,
including the read-only Library search POST, and Picker requests bypass it.

## Picker Usage

Request `PickerMediaItemsReadOnly` and resolve `GooglePhotosPickerService` from the same container.
Call `LoginAsync` once, then:

1. `CreateSessionAsync` returns a `PickerUri` and polling configuration.
2. Open that URI outside an iframe in a browser signed into the session's Google account.
3. Poll `GetSessionAsync` at Google's `PollInterval`, respecting `TimeoutIn`.
4. Once `MediaItemsSet` is true, enumerate `GetMediaItemsAsync(sessionId)`.
5. Stream selected bytes with `DownloadPhotoAsync` or `DownloadVideoAsync`, then delete the session.

Picked-media URLs normally expire after 60 minutes, can expire sooner on revocation, and require
bearer authorization. The download helpers use the authenticated client. Photo downloads require
width and height between 1 and 16383 and can request EXIF metadata.

See the [Picker walkthrough and interaction diagram](https://github.com/f2calv/CasCap.Api.GooglePhotos#picker-api)
and [Google's media access guide](https://developers.google.com/photos/picker/guides/media-items).

## Dependencies

| Package | Purpose |
| --- | --- |
| `Google.Apis.Auth` | OAuth authentication and cached token grants |
| `Microsoft.AspNetCore.WebUtilities` | Query-string construction |
| `Microsoft.Extensions.Http.Resilience` | HTTP timeouts, circuit breaking and safe retries |
| `Microsoft.Extensions.Options.ConfigurationExtensions` | Options binding |
| `Microsoft.Extensions.Options.DataAnnotations` | Options validation |
| `MimeTypeMapOfficial` | Upload MIME-type detection |
| `System.Threading.RateLimiting` | Optional temporal Library API write limiting |
| `CasCap.Common.Net` | Shared HTTP and serialization infrastructure |

## Documentation

- [Google Cloud setup and API-specific scopes](https://github.com/f2calv/CasCap.Api.GooglePhotos#quick-start)
- [Breaking changes when migrating to v4](https://github.com/f2calv/CasCap.Api.GooglePhotos#migrating-to-v4)
- [Generic Host SampleApp](https://github.com/f2calv/CasCap.Api.GooglePhotos/blob/main/samples/CasCap.Api.GooglePhotos.GenericHost.SampleApp/README.md)
- [Direct construction SampleApp](https://github.com/f2calv/CasCap.Api.GooglePhotos/blob/main/samples/CasCap.Api.GooglePhotos.Console.SampleApp/README.md)
- [MIT License](https://github.com/f2calv/CasCap.Api.GooglePhotos/blob/main/LICENSE)
