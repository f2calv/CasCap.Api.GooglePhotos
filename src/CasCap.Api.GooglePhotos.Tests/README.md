---
title: CasCap.Api.GooglePhotos Tests
description: Integration test structure, authentication requirements, and execution commands.
---

## Purpose

This project exercises the Google Photos client against a dedicated test account. The tests require OAuth credentials from .NET User Secrets and may create albums and upload synthetic media.

## Tests

| Test method                                     | Method count | Test case count | Category    |
| ----------------------------------------------- | ------------ | --------------- | ----------- |
| `DoLogin`                                       | 1            | 1               | Integration |
| `PickerSessionLifecycle`                        | 1            | 1               | Integration |
| `UploadMedia`                                   | 1            | 3               | Integration |
| `UploadSingle`                                  | 1            | 2               | Integration |
| `UploadMultiple`                                | 1            | 1               | Integration |
| `FilterMediaItems`                              | 1            | 1               | Integration |
| `AddEnrichments`                                | 1            | 1               | Integration |
| `DownloadBytes`                                 | 1            | 9               | Integration |
| `CreateSessionRejectsInvalidItemCount`          | 1            | 2               | Picker      |
| `CreateSessionRejectsNonVersionFourRequestId`   | 1            | 1               | Picker      |
| `DeleteSessionWrapsMalformedError`              | 1            | 1               | Picker      |
| `DownloadPhotoIncludesDimensionsAndExif`        | 1            | 1               | Picker      |
| `DownloadPhotoWrapsMalformedError`              | 1            | 1               | Picker      |
| `GetMediaItemsFollowsPageToken`                 | 1            | 1               | Picker      |
| `RegistrationRejectsEmptyScopes`                | 1            | 1               | Picker      |
| Total                                           | 15           | 27              |             |

## Trait Categories

Credentialed tests carry `Category=Integration`; Picker unit tests carry `Category=Picker`. API-specific integration coverage carries `Type=GooglePhotosService` or `Type=GooglePhotosPickerService`.

## Skipped Tests

`DoLogin` uses `SkipIfCIBuildFact` and is skipped in CI because local OAuth authentication may require browser interaction. No tests are unconditionally skipped.

## Authentication

Store test credentials in .NET User Secrets. Never add account identifiers, OAuth credentials, access tokens, or cached OAuth responses to tracked files.

## Running Tests

Ask before running integration tests. Microsoft.Testing.Platform supports precise method filtering and explicit live xUnit output:

```powershell
dotnet test --project .\src\CasCap.Api.GooglePhotos.Tests\CasCap.Api.GooglePhotos.Tests.csproj --filter-method CasCap.Tests.GooglePhotosIntegrationTests.DoLogin --show-live-output on --xunit-info
```

## File Structure

```text
CasCap.Api.GooglePhotos.Tests/
|-- Tests/
|   |-- Integration/
|   |   |-- GooglePhotosIntegrationTests.cs
|   |   `-- TestBase.cs
|   |-- Unit/
|   |   `-- GooglePhotosPickerServiceTests.cs
|   `-- ExifTests.cs        # TODO: re-enable the EXIF metadata proof
|-- testdata/
|-- appsettings.Test.json
|-- GlobalUsings.cs
`-- CasCap.Api.GooglePhotos.Tests.csproj
```

## Dependencies

| NuGet package                               | Purpose                                |
| ------------------------------------------- | -------------------------------------- |
| `xunit.v3`                                  | Test framework and MTP integration     |
| `Microsoft.Testing.Extensions.CodeCoverage` | Code coverage collection               |
| `Microsoft.Extensions.Configuration.*`      | JSON, User Secrets, and environment    |
| `CasCap.Common.Testing`                     | xUnit logging and test infrastructure  |

| Project reference               | Purpose                          |
| ------------------------------- | -------------------------------- |
| `CasCap.Api.GooglePhotos`       | Library under test               |
| `CasCap.Common.Testing` (Debug) | Local shared test infrastructure |
