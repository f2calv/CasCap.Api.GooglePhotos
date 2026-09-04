---
title: CasCap.Api.GooglePhotos Tests
description: Integration test structure, authentication requirements, and execution commands.
---

## Purpose

This project exercises the Google Photos client against a dedicated test account. The tests require OAuth credentials from .NET User Secrets and may create albums and upload synthetic media.

## Tests

| Test method                                    | Method count | Test case count | Category      |
| ---------------------------------------------- | ------------ | --------------- | ------------- |
| `DoLogin`                                      | 1            | 1               | Integration   |
| `PickerSessionLifecycle`                       | 1            | 1               | Integration   |
| `UploadMedia`                                  | 1            | 3               | Integration   |
| `UploadSingle`                                 | 1            | 2               | Integration   |
| `UploadMultiple`                               | 1            | 1               | Integration   |
| `FilterMediaItems`                             | 1            | 1               | Integration   |
| `AddEnrichments`                               | 1            | 1               | Integration   |
| `DownloadBytes`                                | 1            | 1               | Integration   |
| `CreateSession_RejectsInvalidItemCount`        | 1            | 2               | Picker        |
| `CreateSession_RejectsNonVersionFourRequestId` | 1            | 1               | Picker        |
| `DeleteSession_EscapesSessionId`               | 1            | 1               | Picker        |
| `DeleteSession_WrapsMalformedError`            | 1            | 1               | Picker        |
| `DownloadPhoto_IncludesDimensionsAndExif`      | 1            | 1               | Picker        |
| `DownloadPhoto_RejectsInvalidDimensions`       | 1            | 4               | Picker        |
| `DownloadPhoto_WrapsMalformedError`            | 1            | 1               | Picker        |
| `DownloadVideo_RequestsRawBytes`               | 1            | 1               | Picker        |
| `GetMediaItems_FollowsPageToken`               | 1            | 1               | Picker        |
| `GetMediaItems_RejectsInvalidPageSize`         | 1            | 2               | Picker        |
| `GetSession_ReturnsSession`                    | 1            | 1               | Picker        |
| `DisabledLimiter_BypassesUnsafeRequests`       | 1            | 1               | RateLimiting  |
| `QueuedWrite_HonorsCancellation`               | 1            | 1               | RateLimiting  |
| `ReadOnlySearch_BypassesLimiter`               | 1            | 1               | RateLimiting  |
| `SafeMethod_BypassesLimiter`                   | 1            | 4               | RateLimiting  |
| `UnsafeMethod_ConsumesPermit`                  | 1            | 4               | RateLimiting  |
| `Filter_UsesGoogleWireNames`                   | 1            | 1               | Serialization |
| `MediaItem_MapsGoogleWireNames`                | 1            | 1               | Serialization |
| `Responses_MapGoogleWireNames`                 | 1            | 1               | Serialization |
| `Registration_CopiesEveryOption`               | 1            | 1               | Registration  |
| `Registration_IsIdempotent`                    | 1            | 1               | Registration  |
| `Registration_IsolatesRateLimitOptions`        | 1            | 1               | Registration  |
| `Registration_IsolatesScopes`                  | 1            | 1               | Registration  |
| `Registration_RejectsEmptyScopes`              | 1            | 1               | Registration  |
| `Registration_RejectsInvalidRateLimit`         | 1            | 1               | Registration  |
| `Registration_RejectsInvalidRequestTimeout`    | 1            | 2               | Registration  |
| `Registration_RejectsInvalidUploadTimeout`     | 1            | 2               | Registration  |
| `Registration_RejectsNullRateLimit`            | 1            | 1               | Registration  |
| `Registration_RejectsNullScopes`               | 1            | 1               | Registration  |
| `Registration_ResolvesTypedClients`            | 1            | 1               | Registration  |
| `Authorization_IsSharedByEveryResolvedClient`  | 1            | 1               | Authorization |
| `Handler_AppliesSuppliedAuthorization`         | 1            | 1               | Authorization |
| `Handler_KeepsCallerSuppliedAuthorization`     | 1            | 1               | Authorization |
| `Handler_SendsNoAuthorizationBeforeLogin`      | 1            | 1               | Authorization |
| `AddMediaItemsToAlbum_DeduplicatesAndBatches`  | 1            | 1               | Library       |
| `AddEnrichmentToAlbum_ReturnsCreatedItem`      | 1            | 1               | Library       |
| `AddMediaItems_BatchesCreationRequests`        | 1            | 1               | Library       |
| `AddMediaItemsToAlbum_HonorsCancellation`      | 1            | 1               | Library       |
| `AddMediaItemsToAlbum_WrapsApiError`           | 1            | 1               | Library       |
| `AddMediaItem_RejectsInvalidAlbumPosition`     | 1            | 3               | Library       |
| `DownloadBytes_BuildsPhotoParameters`          | 1            | 1               | Library       |
| `DownloadBytes_BuildsVideoParameters`          | 1            | 1               | Library       |
| `DownloadBytes_WrapsApiError`                  | 1            | 1               | Library       |
| `GetAlbum_WrapsApiError`                       | 1            | 1               | Library       |
| `GetAlbums_RejectsInvalidPageSize`             | 1            | 2               | Library       |
| `GetAlbums_SendsRequestedPageSize`             | 1            | 3               | Library       |
| `GetMediaItems_DeduplicatesAcrossPages`        | 1            | 1               | Library       |
| `GetMediaItemById_WrapsApiError`               | 1            | 1               | Library       |
| `GetMediaItemsByFilter_RemovesEmptyFilters`    | 1            | 1               | Library       |
| `GetMediaItemsByIds_SkipsFailedResults`        | 1            | 1               | Library       |
| `GetMediaItems_RejectsInvalidPageSize`         | 1            | 2               | Library       |
| `GetMediaItems_SendsRequestedPageSize`         | 1            | 3               | Library       |
| `GetOrCreateAlbum_CreatesMissingAlbum`         | 1            | 1               | Library       |
| `GetOrCreateAlbum_ReturnsExistingAlbum`        | 1            | 1               | Library       |
| `IsFileUploadableByExtension_ClassifiesTypes`  | 1            | 4               | Library       |
| `Login_RejectsUndefinedScope`                  | 1            | 1               | Library       |
| `GetTokenStoreKey_IncludesClientId`            | 1            | 1               | Library       |
| `RemoveMediaItemsFromAlbum_BatchesRequests`    | 1            | 1               | Library       |
| `UploadMedia_RecoversFromAcceptedChunk`        | 1            | 1               | Library       |
| `UploadMedia_RecoversFromAcceptedFinalChunk`   | 1            | 1               | Library       |
| `UploadMedia_RecoversResumableSingle`          | 1            | 1               | Library       |
| `UploadMedia_WrapsMalformedError`              | 1            | 3               | Library       |
| `IsUploadRequest_MatchesProtocolHeaders`       | 1            | 4               | Library       |
| Total                                          | 71           | 103             |               |

## Trait Categories

Credentialed tests carry `Category=Integration`; unit tests carry `Category=Authorization`, `Category=Library`, `Category=Picker`, `Category=RateLimiting`, `Category=Registration`, or `Category=Serialization`. API-specific integration coverage carries `Type=GooglePhotosService` or `Type=GooglePhotosPickerService`.

## Skipped Tests

`DoLogin` uses `SkipIfCIBuildFact` and is skipped in CI because local OAuth authentication may require browser interaction. No tests are unconditionally skipped.

## Authentication

Store test credentials in .NET User Secrets. Never add account identifiers, OAuth credentials, access tokens, or cached OAuth responses to tracked files.

## Running Tests

CI runs the credential-free tests on every push, excluding the integration category:

```powershell
dotnet test --project .\src\CasCap.Api.GooglePhotos.Tests\CasCap.Api.GooglePhotos.Tests.csproj --filter-not-trait Category=Integration
```

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
|   |   |-- GooglePhotosAuthorizationTests.cs
|   |   |-- GooglePhotosPickerServiceTests.cs
|   |   |-- GooglePhotosServiceTests.cs
|   |   |-- GooglePhotosWriteRateLimitingHandlerTests.cs
|   |   |-- ModelSerializationTests.cs
|   |   `-- ServiceCollectionExtensionsTests.cs
|   |-- StubHttpMessageHandler.cs
|   `-- TempMediaFile.cs
|-- testdata/
|-- appsettings.Test.json
|-- GlobalUsings.cs
`-- CasCap.Api.GooglePhotos.Tests.csproj
```

## Dependencies

| NuGet package                               | Purpose                               |
| ------------------------------------------- | ------------------------------------- |
| `xunit.v3`                                  | Test framework and MTP integration    |
| `Microsoft.Testing.Extensions.CodeCoverage` | Code coverage collection              |
| `Microsoft.Extensions.Configuration.*`      | JSON, User Secrets, and environment   |
| `CasCap.Common.Testing`                     | xUnit logging and test infrastructure |

| Project reference               | Purpose                          |
| ------------------------------- | -------------------------------- |
| `CasCap.Api.GooglePhotos`       | Library under test               |
| `CasCap.Common.Testing` (Debug) | Local shared test infrastructure |
