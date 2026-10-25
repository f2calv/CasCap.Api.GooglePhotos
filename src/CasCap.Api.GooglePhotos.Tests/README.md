# CasCap.Api.GooglePhotos.Tests

xUnit v3 tests for the Google Photos Library and Picker clients, using Microsoft.Testing.Platform
on .NET 10. Unit tests use fake HTTP responses; integration tests require a Google account and can
create albums, upload media and change album contents.

## Tests

Counts are based on source declarations and expanded `InlineData` rows, not a test run.

| Test class | Methods | Expanded cases | Category | Scope |
| --- | ---: | ---: | --- | --- |
| `GooglePhotosAuthorizationTests` | 4 | 4 | `Authorization` | Shared authorization and caller-owned headers |
| `GooglePhotosPickerServiceTests` | 11 | 16 | `Picker` | Sessions, paging, parameter validation and downloads |
| `GooglePhotosServiceTests` | 29 | 45 | `Library` | Album/media operations, batching, uploads, recovery and errors |
| `GooglePhotosWriteRateLimitingHandlerTests` | 5 | 11 | `RateLimiting` | Write permits, bypasses and cancellation |
| `ModelSerializationTests` | 3 | 3 | `Serialization` | Google JSON wire-name mappings |
| `ServiceCollectionExtensionsTests` | 11 | 13 | `Registration` | Options copying, validation and client registration |
| `GooglePhotosIntegrationTests` | 9 | 12 | `Integration` | Credentialed Library operations and Picker session lifecycle |
| **Total** | **72** | **104** | | |

The credential-free unit subset contains **63 methods / 92 cases**. Integration contains
**9 methods / 12 cases**. Those seven `Category` values are all category traits.
`UploadMedia` additionally has `Type=GooglePhotosService`; `PickerSessionLifecycle` has
`Type=GooglePhotosPickerService`.

The Picker integration test creates, retrieves and deletes a session. It does **not** complete an
interactive selection or download real picked media; download and paging coverage comes from unit
test HTTP fakes. These inventories do not establish live API availability or granted account access.

## Skipped Tests

| Declaration | Count | Condition/reason |
| --- | ---: | --- |
| `DoLogin` / `SkipIfCIBuildFact` | 1 method / 1 case | Conditional CI-build skip for browser-based local authentication |
| Unconditional skips | 0 | None declared |

Other integration tests are not automatically skipped for missing credentials. Excluding
`Category=Integration` is a selection filter, not a declared skip.

## Prerequisites and Authentication

Use a stable .NET 10 SDK. Release configuration consumes published dependencies; Debug additionally
requires an adjacent [CasCap.Common checkout](https://github.com/f2calv/CasCap.Common).

Integration configuration loads, in order:

1. Required `appsettings.json` and optional `appsettings.Test.json`.
2. Optional `appsettings.Local.json`, `appsettings.Local.Test.json` and `appsettings.Tests.Local.json`.
3. The test project's .NET User Secrets.
4. Environment variables.

The shared configuration loader selects the `Test` environment; the Test file may be absent when
another provider supplies the required values.

Configure `CasCap:GooglePhotosOptions:User`, `ClientId` and `ClientSecret` privately. The tracked
configuration requests all four supported Photos scopes. Enable both Google Photos APIs and
configure Desktop OAuth consent as described in the [root setup guide](../../README.md#quick-start).
Use a dedicated account and synthetic media; tests upload files from `testdata/` and can leave
created media and albums behind.

Local integration calls use `LoginAsync` and may open a browser. When `TF_BUILD` or `GITHUB_ACTIONS`
is present, integration authentication instead requires a secret-backed
`GOOGLE_PHOTOS_ACCESS_TOKEN`; that supplied token is not refreshed by the client.

The GenericHost sample and test project currently share a User Secrets store. Setting credentials
through either project changes the values seen by both; do not assume the sample and tests use
different accounts. Keep cached OAuth grants and credentials out of source control and test output.

## Running Tests

Run only after explicit approval. From the repository root, the credential-free selection is:

```powershell
dotnet test --project .\src\CasCap.Api.GooglePhotos.Tests\CasCap.Api.GooglePhotos.Tests.csproj --configuration Release --filter-not-trait Category=Integration
```

Remove the filter only for an explicitly approved integration run with the prerequisites above.
Google's app-created-content restrictions apply to these tests as they do to consumers.

## Layout

```text
Tests/
├── Integration/
│   ├── GooglePhotosIntegrationTests.cs
│   └── TestBase.cs
└── Unit/
    ├── GooglePhotosAuthorizationTests.cs
    ├── GooglePhotosPickerServiceTests.cs
    ├── GooglePhotosServiceTests.cs
    ├── GooglePhotosWriteRateLimitingHandlerTests.cs
    ├── ModelSerializationTests.cs
    └── ServiceCollectionExtensionsTests.cs
```

`TestBase` owns configuration, logging, DI and service disposal. Media fixtures live in `testdata/`;
`MetadataExtractor` inspects EXIF during integration download tests.
