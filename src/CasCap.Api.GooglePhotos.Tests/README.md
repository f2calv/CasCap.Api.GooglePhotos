---
title: CasCap.Api.GooglePhotos Tests
description: Integration test structure, authentication requirements, and execution commands.
---

## Purpose

This project exercises the Google Photos client against a dedicated test account. The tests require OAuth credentials from .NET User Secrets and may create albums and upload synthetic media.

## Tests

| Test method                                     | Method count | Test case count | Category      |
| ----------------------------------------------- | ------------ | --------------- | ------------- |
| `DoLogin`                                       | 1            | 1               | Integration   |
| `PickerSessionLifecycle`                        | 1            | 1               | Integration   |
| `UploadMedia`                                   | 1            | 3               | Integration   |
| `UploadSingle`                                  | 1            | 2               | Integration   |
| `UploadMultiple`                                | 1            | 1               | Integration   |
| `FilterMediaItems`                              | 1            | 1               | Integration   |
| `AddEnrichments`                                | 1            | 1               | Integration   |
| `DownloadBytes`                                 | 1            | 1               | Integration   |
| `CreateSessionRejectsInvalidItemCount`          | 1            | 2               | Picker        |
| `CreateSessionRejectsNonVersionFourRequestId`   | 1            | 1               | Picker        |
| `DeleteSessionEscapesSessionId`                 | 1            | 1               | Picker        |
| `DeleteSessionWrapsMalformedError`              | 1            | 1               | Picker        |
| `DownloadPhotoIncludesDimensionsAndExif`        | 1            | 1               | Picker        |
| `DownloadPhotoRejectsInvalidDimensions`         | 1            | 4               | Picker        |
| `DownloadPhotoWrapsMalformedError`              | 1            | 1               | Picker        |
| `DownloadVideoRequestsRawBytes`                 | 1            | 1               | Picker        |
| `GetMediaItemsFollowsPageToken`                 | 1            | 1               | Picker        |
| `GetMediaItemsRejectsInvalidPageSize`           | 1            | 2               | Picker        |
| `GetSessionReturnsSession`                      | 1            | 1               | Picker        |
| `DisabledLimiterBypassesUnsafeRequests`         | 1            | 1               | RateLimiting  |
| `QueuedWriteHonorsCancellation`                 | 1            | 1               | RateLimiting  |
| `ReadOnlySearchBypassesLimiter`                 | 1            | 1               | RateLimiting  |
| `SafeMethodBypassesLimiter`                     | 1            | 4               | RateLimiting  |
| `UnsafeMethodConsumesPermit`                    | 1            | 4               | RateLimiting  |
| `FilterUsesGoogleWireNames`                     | 1            | 1               | Serialization |
| `MediaItemMapsGoogleWireNames`                  | 1            | 1               | Serialization |
| `ResponsesMapGoogleWireNames`                   | 1            | 1               | Serialization |
| `RegistrationCopiesEveryOption`                 | 1            | 1               | Registration  |
| `RegistrationIsIdempotent`                      | 1            | 1               | Registration  |
| `RegistrationIsolatesRateLimitOptions`          | 1            | 1               | Registration  |
| `RegistrationIsolatesScopes`                    | 1            | 1               | Registration  |
| `RegistrationRejectsEmptyScopes`                | 1            | 1               | Registration  |
| `RegistrationRejectsInvalidRateLimit`           | 1            | 1               | Registration  |
| `RegistrationRejectsInvalidRequestTimeout`      | 1            | 2               | Registration  |
| `RegistrationRejectsInvalidUploadTimeout`       | 1            | 2               | Registration  |
| `RegistrationRejectsNullRateLimit`              | 1            | 1               | Registration  |
| `RegistrationRejectsNullScopes`                 | 1            | 1               | Registration  |
| `RegistrationResolvesTypedClients`              | 1            | 1               | Registration  |
| `AuthorizationIsSharedByEveryResolvedClient`    | 1            | 1               | Authorization |
| `HandlerAppliesSuppliedAuthorization`           | 1            | 1               | Authorization |
| `HandlerKeepsCallerSuppliedAuthorization`       | 1            | 1               | Authorization |
| `HandlerSendsNoAuthorizationBeforeLogin`        | 1            | 1               | Authorization |
| `AddMediaItemsToAlbumDeduplicatesAndBatches`    | 1            | 1               | Library       |
| `AddEnrichmentToAlbumReturnsCreatedItem`        | 1            | 1               | Library       |
| `AddMediaItemsBatchesCreationRequests`          | 1            | 1               | Library       |
| `AddMediaItemsToAlbumHonorsCancellation`        | 1            | 1               | Library       |
| `AddMediaItemsToAlbumWrapsApiError`             | 1            | 1               | Library       |
| `AddMediaItemRejectsInvalidAlbumPosition`       | 1            | 3               | Library       |
| `DownloadBytesBuildsPhotoParameters`            | 1            | 1               | Library       |
| `DownloadBytesBuildsVideoParameters`            | 1            | 1               | Library       |
| `DownloadBytesWrapsApiError`                    | 1            | 1               | Library       |
| `GetAlbumWrapsApiError`                         | 1            | 1               | Library       |
| `GetAlbumsRejectsInvalidPageSize`               | 1            | 2               | Library       |
| `GetAlbumsSendsRequestedPageSize`               | 1            | 3               | Library       |
| `GetMediaItemsDeduplicatesAcrossPages`          | 1            | 1               | Library       |
| `GetMediaItemByIdWrapsApiError`                 | 1            | 1               | Library       |
| `GetMediaItemsByFilterRemovesEmptyFilters`      | 1            | 1               | Library       |
| `GetMediaItemsByIdsSkipsFailedResults`          | 1            | 1               | Library       |
| `GetMediaItemsRejectsInvalidPageSize`           | 1            | 2               | Library       |
| `GetMediaItemsSendsRequestedPageSize`           | 1            | 3               | Library       |
| `GetOrCreateAlbumCreatesMissingAlbum`           | 1            | 1               | Library       |
| `GetOrCreateAlbumReturnsExistingAlbum`          | 1            | 1               | Library       |
| `IsFileUploadableByExtensionClassifiesTypes`    | 1            | 4               | Library       |
| `LoginRejectsUndefinedScope`                    | 1            | 1               | Library       |
| `OAuthCacheKeyIncludesClientId`                 | 1            | 1               | Library       |
| `RemoveMediaItemsFromAlbumBatchesRequests`      | 1            | 1               | Library       |
| `UploadMediaRecoversFromAcceptedChunk`          | 1            | 1               | Library       |
| `UploadMediaRecoversFromAcceptedFinalChunk`     | 1            | 1               | Library       |
| `UploadMediaRecoversResumableSingle`            | 1            | 1               | Library       |
| `UploadMediaWrapsMalformedError`                | 1            | 3               | Library       |
| `UploadRequestDetectionMatchesProtocolHeaders`  | 1            | 4               | Library       |
| Total                                           | 71           | 103             |               |

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
|   `-- StubHttpMessageHandler.cs
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
