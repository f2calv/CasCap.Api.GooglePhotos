---
description: 'README consistency and Mermaid diagram conventions for Markdown documentation.'
applyTo: '**/*.md'
---

# Documentation

## README Consistency

- Every project must have a `README.md`. When adding a `.csproj`, create its README in the project directory in the same change. Follow the existing sequence: Purpose, Services or Extensions, Configuration, and Dependencies.
- Keep each project README synchronized with its implementation. During refactoring, check affected READMEs for stale type names, configuration options, dependencies, and diagrams.
- Update README references in the same change as project moves, DI restructuring, model splits, and public type renames.
- Use spaced Markdown table separators, such as `| --- | --- |`, to satisfy MD060.
- Libraries exposing options records should document minimal and complete `appsettings.json` examples. Credentials must remain placeholders and secret-storage guidance must accompany sensitive fields.

## Mermaid Diagrams

Use Mermaid diagrams to explain relationships and flows that are difficult to scan as prose:

- Use `flowchart` for sequential processing, data flow, service orchestration, and CI/CD pipelines.
- Use `graph` for dependencies and non-sequential relationships.
- Use `classDiagram` for inheritance, composition, and interface implementation.
- Use `sequenceDiagram` for time-ordered calls and async interactions.

Use standard headings such as `## Data Flow`, `## Service Architecture`, `## Dependency Graph`, `## Class Hierarchy`, `## Deployment Flow`, and `## Configuration Hierarchy`.

## Synchronization

- Keep diagrams synchronized with code during refactoring.
- Update diagram nodes when services or public types are renamed.
- Update dependency graphs when package or project references change.
- Review affected README diagrams before creating a pull request.