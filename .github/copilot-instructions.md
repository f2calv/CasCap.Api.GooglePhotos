# Copilot Instructions

## Shared Instructions

Shared Copilot instruction files are maintained centrally in the [.github](https://github.com/f2calv/.github) repository under `instructions/`, and are applied to every workspace from the VS Code user profile via `~/.copilot/instructions`. They are deliberately not copied into this repository, so a change there takes effect everywhere without a pull request here.

Everything below is specific to this repository.

## Repository Purpose

This repository is a .NET client library for the Google Photos APIs, with console and Generic Host samples. Authentication configuration and cached OAuth tokens are sensitive and must never be committed.

## Google Photos Logging Redaction

Beyond the general secret-redaction rule in `csharp.instructions.md`, never log OAuth client secrets, access tokens, refresh tokens, cached token file contents, Google account identifiers, or personally identifying media filenames.

The same applies to CI: never print OAuth credentials, NuGet credentials, or Google Photos access tokens in a workflow log or step summary.
