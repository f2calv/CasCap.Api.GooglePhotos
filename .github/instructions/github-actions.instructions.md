---
description: 'GitHub Actions workflow conventions for naming, YAML style, security and GitVersion.'
applyTo: '.github/workflows/**,.github/actions/**,**/action.yml,**/action.yaml'
---

# GitHub Actions

## General

- Leave one blank line between steps within a job.
- Pin actions to their major version tags by default.
- Set `fetch-depth: 0` on checkout when GitVersion needs the full commit history. Lint-only workflows may use `fetch-depth: 1`.
- Set workflow-level `permissions: {}` and grant each job only the permissions it requires.

## Step Naming

- Name one-line `run` steps after the command or a concise abbreviation of it.
- Give multi-step setup names an `(N of M)` suffix.
- Include matrix values in matrix-driven step names.

## Naming Conventions

- Use kebab-case for inputs and outputs.
- Use uppercase snake case for environment variables and secrets.
- Keep every `description:` short and focused on what the value is. Put rationale and caveats in a comment above the input.

## YAML Style

- Use 2-space indentation.
- Do not quote scalars unless YAML requires it. Quote string defaults that represent booleans.
- Use `|` for multi-line scripts and `>` for flowing multi-line descriptions.
- Leave one blank line between major workflow sections and no blank lines within input or output lists.

## Reusability

- Put reusable cross-repository build, test, lint, packaging, and versioning logic in reusable workflows in the shared workflow repository.
- Prefix repository-local reusable workflow filenames with `_` and call them through `./.github/workflows/_filename.yml`.
- Prefer `secrets: inherit` unless a called workflow needs a narrower secret surface.

## Composite Actions

- Declare `shell: bash` on every composite-action `run` step.
- Resolve scripts from `${{ github.action_path }}`.
- Extract sizeable or critical scripts to `.scripts/` so they can be tested independently.

## Security

- Deny permissions globally and grant the minimum per job.
- Skip bot-triggered operations when credentials or publication are unavailable.
- Pass tokens through standard input for registry logins.
- Never print OAuth credentials, NuGet credentials, or Google Photos access tokens.

## GitVersion

- Use `fetch-depth: 0` when GitVersion runs.
- Keep `GitVersion.yml` at the repository root.
- Prefer `semVer` for tags and releases and `fullSemVer` for build versions and prerelease identifiers.