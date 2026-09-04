# Generic Host Sample

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

Unlike the ConsoleApp sample, the credentials are deliberately not listed in [Properties/launchSettings.json](Properties/launchSettings.json). Environment variables are loaded after User Secrets, so a blank placeholder there would override a secret you had already set and the sample would fail to authenticate.

To use environment variables anyway, supply the whole value in the standard double-underscore form, for example `CasCap__GooglePhotosOptions__ClientId`.

## Walkthrough

[Program.cs](Program.cs) registers the library and a `BackgroundService`. [Services/GooglePhotosWorker.cs](Services/GooglePhotosWorker.cs) does the work in five numbered steps: read the configured media path, authenticate, find or create an album, upload, then read the album back.

## Run

```powershell
dotnet run --project samples/GenericHost
```

The first run opens a browser for Google OAuth consent.
