---
description: 'Google Photos options, appsettings synchronization and secret-handling conventions.'
applyTo: '**/appsettings*.json'
---

# Configuration

## Environment Layering

- Load `appsettings.json` first and then `appsettings.{Environment}.json`; later providers override earlier values by key.
- Load .NET User Secrets after JSON for local development and test credentials.
- Load environment variables last so CI and deployment environments can override JSON and User Secrets.
- Use the standard double-underscore form for nested environment variables, for example `CasCap__GooglePhotosOptions__ClientId`.

## Configuration Synchronization

- `GooglePhotosOptions` defines the `CasCap:GooglePhotosOptions` configuration shape. When adding, renaming, or removing one of its bindable properties, update every applicable `appsettings*.json` file and README example in the same change.
- Environment-specific files only need to repeat values that differ from the base file.
- Keep tracked examples runnable after credentials are supplied, using safe public defaults or `null` placeholders for sensitive values.
- Apply data-annotation validation to required values and preserve configuration validation when changing the options model.

## Secret Safety

- Never commit Google account identifiers, OAuth client IDs, OAuth client secrets, access tokens, refresh tokens, or cached OAuth response files.
- Store local development and test credentials with .NET User Secrets. Supply CI credentials through GitHub Actions secrets and environment variables.
- Treat every file packaged into a NuGet package or sample output as public.
- Keep the Google authentication cache outside the repository. Do not add cached token paths or token contents to tracked examples, logs, tests, or documentation.
