---
title: Direct Console Sample
description: Run CasCap.Api.GooglePhotos through direct service construction without dependency injection.
---

# Direct Console Sample

## Purpose

This sample constructs `GooglePhotosService`, `HttpClient`, logging, and options directly. It creates an album, uploads one media file, and lists that album's contents.

Because it bypasses `AddGooglePhotos`, it also bypasses the resilience pipeline and the optional write rate limiter, and it reads credentials from environment variables rather than the `CasCap:GooglePhotosOptions` configuration section. See the [GenericHost sample](../GenericHost) for the configured path.

## Configuration

Enable the Google Photos Library API and create a Desktop app OAuth client in Google Cloud.

Set the required credentials as environment variables. Do not place OAuth credentials in source code.

```powershell
$env:GOOGLE_PHOTOS_USER = "user@example.com"
$env:GOOGLE_PHOTOS_CLIENT_ID = "your-client-id"
$env:GOOGLE_PHOTOS_CLIENT_SECRET = "your-client-secret"
```

When debugging, fill in the blank placeholders in [Properties/launchSettings.json](Properties/launchSettings.json) instead, along with `commandLineArgs` for the media file path. That file is tracked, so leave the placeholders empty when committing.

## Walkthrough

[Program.cs](Program.cs) is written as twelve numbered steps so the hand-built pipeline can be read top to bottom:

1. Read the media file argument.
2. Wire Ctrl+C to a `CancellationToken`.
3. Create a logger factory.
4. Build `GooglePhotosOptions` and choose OAuth scopes.
5. Report where the OAuth grant is cached, which explains when the browser opens.
6. Create the `HttpClientHandler`.
7. Create the credential provider and its authorization handler.
8. Construct `GooglePhotosService`.
9. Authenticate.
10. Find or create an album.
11. Upload the media file.
12. List the album contents.

## Run

Pass an existing photo or video path after `--`:

```powershell
dotnet run --project samples/ConsoleApp -- "C:\media\photo.jpg"
```

The first run opens a browser for Google OAuth consent.
