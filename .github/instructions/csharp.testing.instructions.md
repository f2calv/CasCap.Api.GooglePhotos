---
description: 'xUnit test structure, authentication, naming, theories and assertion conventions.'
applyTo: '**/*Tests/**/*.cs'
---

# Testing

## Folder Structure

Organize each `*.Tests` project by test type:

```text
Tests/
|-- Unit/           # Self-contained tests with no external services
`-- Integration/    # Tests requiring configuration or Google Photos access
    `-- TestBase.cs # Shared integration-test setup
```

## Integration Tests

- Apply `[Trait("Category", "Integration")]` to tests that call Google Photos or depend on credentials.
- Place integration tests under `Tests/Integration/` and share configuration, logging, and service setup through `TestBase`.
- Build configuration in repository order: `appsettings.Test.json`, User Secrets for local runs, then environment variables for CI.
- Dispose any `ServiceProvider` created by the fixture through `IDisposable` or `IAsyncDisposable`.
- Local authentication may require browser interaction. Never initiate interactive login in CI.
- Read CI access tokens only from secret-backed environment variables. Never commit credentials or cached OAuth responses, and never write resolved secrets to test output.
- Do not run integration tests without explicit user approval.

## Unit Tests

- Place self-contained tests under `Tests/Unit/`.
- Do not inherit unit tests from an integration `TestBase` or require Google credentials.
- Use domain-specific trait categories such as `Parsing` or `Serialization`, rather than `Unit`.

## Diagnostic Output

- Write diagnostics through `ITestOutputHelper` or an `ILogger` configured to route output to xUnit.
- Never use `Debug.WriteLine` or `Console.WriteLine`; their output is unreliable in CI and the test explorer.
- Prefer interpolation over concatenation or composite formatting.
- Emit only values that explain a failure, and include decisive context in assertion messages.

## Microsoft.Testing.Platform Follow-up

- `global.json` selects `Microsoft.Testing.Platform` for `dotnet test`.
- Before modernizing the tests, verify console-output visibility and `ITestOutputHelper` behavior with the repository's exact .NET SDK, xUnit v3, and Microsoft.Testing.Platform versions. Agents may otherwise misread quiet command output as no test execution.
- Record the commands, verbosity settings, discovered-test counts, passed/failed/skipped totals, and output behavior during that investigation.
- Do not remove `ITestOutputHelper` or change test runners solely because of an unverified limitation; base the decision on the repository-specific investigation.

## Theory Parameterization

- Consolidate facts that differ only by input into one `[Theory]` with `[InlineData]`.
- Keep `[Fact]` for tests whose setup or assertions do not parameterize cleanly.

## Test Method Naming

- Name test methods after the method or feature under test, such as `UploadMedia` or `ParsesExifMetadata`.
- Do not use verbose BDD-style sentence names.
- Use underscore-separated phases for lifecycle tests, such as `AlbumLifecycle_CreateGetUpdateDelete`.

## Assertions

- Every test must contain meaningful assertions. Never use `Assert.True(true)` or another placeholder assertion.
- Prefer specific assertions such as `Assert.Equal`, `Assert.Contains`, `Assert.Single`, and `Assert.NotNull` over `Assert.True(condition)`.
- Move performance-only tests to BenchmarkDotNet rather than retaining timing loops without correctness assertions.

## Dead Code

- Delete commented-out tests, unreachable branches, and permanently skipped tests rather than leaving them in the suite.
- Remove helpers, fields, and using directives when their last test consumer is removed.

## Shared Test Data

- Keep shared generators and fixtures in dedicated `*TestData.cs` files at the `Tests/` root.
- Keep hardcoded regression data in `*Patterns.cs` files.
- Put stateless object-building helpers in static classes.

## Test Project README

Each test-project README must include the method count, expanded test-case count, trait categories, skipped-test reasons, and a diagram of the `Tests/` layout.
