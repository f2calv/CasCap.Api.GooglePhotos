# Copilot Instructions

<!-- Synced section ------------------------------------------------------
	This file plus the shared files under `.github/instructions/` are kept
	aligned across f2calv .NET repositories. The repo-specific
	"Project-Specific Overrides" section below is excluded from sync.
	Edit once, sync everywhere.
	------------------------------------------------------------------- -->

## Instruction Files

Detailed conventions live in scoped instruction files under `.github/instructions/`, auto-applied by file type:

| File | Applies to | Covers |
| --- | --- | --- |
| `csharp.instructions.md` | `**/*.cs` | C# / .NET style, XML docs, logging, performance, Web API |
| `csharp.testing.instructions.md` | `**/*Tests/**/*.cs` | xUnit test structure, naming, theories, assertions |
| `dotnet.instructions.md` | `**/*.csproj`, `*.slnx`, `Directory.*.props` | Central build/package config, solution format, SDK selection |
| `github-actions.instructions.md` | workflows / `action.yml` | GitHub Actions naming, YAML, security, GitVersion |
| `bash.instructions.md` | `**/*.sh` | Bash scripting structure, error handling, logging, testability |
| `documentation.instructions.md` | `**/*.md` | README consistency and Mermaid diagrams |
| `configuration.instructions.md` | `**/appsettings*.json` | Options/appsettings synchronization and secret safety |

The conventions below always apply, regardless of the file being edited.

## Copilot Workflow

- **Test execution**: Never run tests automatically; they may be integration tests requiring Google Photos credentials and interactive authentication. Always prompt (ideally with a visual yes/no button) before running any tests.
- **Preserve git history during renames/moves**: When renaming or relocating files, first perform the rename/move (preferably via `git mv`), then make content edits to the file at its new path. Do not delete and recreate files when a rename or move is intended.
- **Multi-repo commits**: When a single change spans multiple repositories, separate per-repository commit messages are acceptable (but not mandatory). Prefer them where the changes are disconnected, or where one repository should not know about the other.
- **Build after refactoring**: After any refactoring, build the entire solution (not only the affected project) to catch compilation errors in dependent projects. When multiple solutions exist, prefer `CasCap.Api.GooglePhotos.Debug.slnx`.

## Public Repository Confidentiality

- Treat every non-public repository's identity and contents as confidential, even when they appear in the local workspace, conversation context, diffs, logs, or tool output.
- Never publish private repository names, URLs, owner/repository coordinates, branches, file paths, architecture, deployment details, or inferred existence in tracked files, commit messages, issues, pull request titles/descriptions/reviews/comments, release notes, workflow annotations, examples, or other public-facing content.
- Describe required relationships generically (for example, "private GitOps repository" or "internal service") and supply private coordinates only through secrets, repository variables, or caller-provided values.
- Before creating or updating public GitHub content, review the proposed text and metadata for private identifiers and implementation details.

## Repository Structure

Every f2calv repository follows a consistent layout, regardless of language:

- Root files include `README.md`, `LICENSE`, `GitVersion.yml`, `.editorconfig`, `.gitattributes`, `.gitignore`, and `.pre-commit-config.yaml`.
- Source code lives under `src/`; runnable examples live under `samples/`.
- Tooling lives in dot-prefixed folders such as `.github/`, `.scripts/`, `.devcontainer/`, `.docker/`, `.config/`, and `.vscode/`.
- Additional documentation beyond the root `README.md` lives as Markdown under `docs/`.
- `.editorconfig` is the source of truth for indentation, line endings, and analyzer or formatting rules.
- `GitVersion.yml` in the root drives semantic-versioning rules.

## Miscellaneous

- When detecting new conventions or patterns, add them to the appropriate `.github/instructions/*.instructions.md` file (or this file for cross-cutting workflow rules) and apply them retroactively where applicable.
- Keep this file and the shared `.github/instructions/` files aligned with the common guidelines used by sibling .NET repositories.

---

## Project-Specific Overrides

### Repository Purpose

This repository is a .NET client library for the Google Photos APIs, with console and Generic Host samples. Authentication configuration and cached OAuth tokens are sensitive and must never be committed.
