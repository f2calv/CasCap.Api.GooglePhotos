---
title: Direct Console Sample
description: Run CasCap.Api.GooglePhotos through direct service construction without dependency injection.
---

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

## Run

Pass an existing photo or video path after `--`:

```powershell
dotnet run --project samples/ConsoleApp -- "C:\media\photo.jpg"
```

The first run opens a browser for Google OAuth consent.
