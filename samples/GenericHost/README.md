---
title: Generic Host Sample
description: Run CasCap.Api.GooglePhotos with standard .NET configuration and dependency injection.
---

## Purpose

This sample uses `Host.CreateApplicationBuilder`, `AddGooglePhotos`, options validation, dependency injection, and a `BackgroundService`. It creates an album, uploads one media file, and reports the number of media items in that album.

## Configuration

Enable the Google Photos Library API and create a Desktop app OAuth client in Google Cloud.

Store local values with .NET User Secrets:

```powershell
dotnet user-secrets set "CasCap:GooglePhotosOptions:User" "user@example.com" --project samples/GenericHost
dotnet user-secrets set "CasCap:GooglePhotosOptions:ClientId" "your-client-id" --project samples/GenericHost
dotnet user-secrets set "CasCap:GooglePhotosOptions:ClientSecret" "your-client-secret" --project samples/GenericHost
dotnet user-secrets set "Sample:MediaPath" "C:\media\photo.jpg" --project samples/GenericHost
```

## Run

```powershell
dotnet run --project samples/GenericHost
```

The first run opens a browser for Google OAuth consent.
