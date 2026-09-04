---
description: 'README consistency and Mermaid diagram conventions for Markdown documentation.'
applyTo: '**/*.md'
---

# Documentation

## README Consistency

- **Every project must have a `README.md`**: When adding a new `.csproj` project, create a `README.md` in the project directory as part of the same commit. Follow the existing pattern: Purpose → Services/Extensions → Configuration → Dependencies (NuGet packages table + Project references table).
- Every project's `README.md` must stay in sync with its implementation. During any refactoring — and **always** before creating a new PR — scan each affected project's `README.md` for inconsistencies: outdated service names, missing or removed configuration options, stale dependency tables, or inaccurate flow diagrams. Update the README as part of the same change, not as a follow-up.
- **Major refactorings** (renames, project moves, DI restructuring, model type splits): when a rename or restructure touches type names, configuration sections, or project references, update every `README.md` that mentions the old names **in the same commit**. Do not leave stale references for a follow-up.
- For large refactorings that touch multiple projects, review all impacted `README.md` files before opening the PR.
- **Markdown tables**: Table separator rows must use spaces around pipes to match the spaced style used in header and data rows (e.g. `| --- | --- |` not `|---|---|`). This prevents MD060 (table-column-style) warnings.
- **Configuration examples in library READMEs**: Library projects that expose `IAppConfig` records should include a `## Configuration Examples` section in their `README.md` with `appsettings.json` snippets progressing from minimal configuration through to fully configured. This documents the configuration surface area and provides copy-paste-ready templates for consumers.
- **Only document what exists**: Do not describe behaviour — encryption, resumability, deduplication, retry semantics, platform support — until the implementation and its tests are in place.
- **Placeholders in examples**: Examples must use synthetic values. Never include real credentials, tokens, connection strings, endpoints, hostnames, device addresses, or personal data.

### Structure and SEO

Apply to every file named `README.md`, wherever it lives — repository root, project, sample, chart, or documentation sub-folder. These rules are specific to `README.md` and do not apply to other Markdown files.

- **Exactly one `# H1`**, as the first content line after any front matter, naming the repository or project. Never repeat the H1 lower down and never open with `##`.
- **Never skip heading levels**: `#` → `##` → `###` in order. Skipping breaks document outline extraction and MD001.
- **Lead with a one- or two-sentence summary** directly under the H1 stating what the thing is and who it is for. Search engines and package registries surface this as the description.
- **Keep the H1 aligned with the package identity** so the README title, the `.csproj` `<Description>`, and the published registry listing agree.
- **Descriptive link text**: Never `click here`, and never a bare URL where a phrase reads better.
- **Alt text on every image** describing the content rather than the file (`![Service dependency graph](…)`, not `![diagram](…)`).
- **Unique headings within a file** so generated anchors resolve predictably.
- **Front matter does not replace the H1**: GitHub renders YAML front matter as a table, not a heading, so a `title:` key alone leaves the page with no `<h1>`. Keep the front matter and add the explicit H1. The repository's `.markdownlint.json` sets `MD025.front_matter_title` to an empty string so the explicit H1 is not reported as a duplicate title.

## Mermaid Diagrams

Use Mermaid diagrams in `README.md` files to visualize complex relationships and flows. Choose the appropriate diagram type:

### Diagram Type Selection

- **`flowchart`**: Sequential processes, data flow, event flow, service orchestration, CI/CD pipelines
  - Direction: Use `TD` (top-down) for vertical flows; `LR` (left-right) for wide workflows
  - Example: Data moving from device → monitor service → broker → processor → sinks

- **`graph`**: Relationships, dependencies, hierarchies (non-sequential)
  - Direction: Use `TD` for dependency trees; `LR` for peer relationships
  - Example: NuGet package dependencies, project references, Helm chart hierarchies

- **`classDiagram`**: C# class hierarchies, inheritance, composition
  - Shows: Inheritance (`<|--`), composition (`*--`), aggregation (`o--`), association (`-->`), dependency (`..>`)
  - Example: Service class inheritance, interface implementation

- **`sequenceDiagram`**: Time-based interactions between components
  - Shows: Method calls, async operations, timing
  - Example: Request/response flows, background service timing

### Standard Headings

Use these consistent heading patterns before Mermaid diagrams:

| Heading | Use For |
| --- | --- |
| `## Data Flow` | How data moves through the system (device → service → storage) |
| `## Event Flow` | Event-driven processing (pub/sub, channels, streams) |
| `## Service Architecture` | How services interact (SignalR hubs, background services, API clients) |
| `## Dependency Graph` | Package/project dependencies, references |
| `## Application Hierarchy` | Nested application or component structures |
| `## Class Hierarchy` | C# class structures, inheritance trees |
| `## Deployment Flow` | CI/CD pipelines, GitHub Actions workflows, Helm/K8s deployments |
| `## Configuration Hierarchy` | IAppConfig structure, nested configuration objects |

### Styling Guidelines

- **Subgraphs**: Group related components (e.g., `subgraph Monitor["MonitorBgService"]`)
- **Custom styling**: Define `classDef` for highlighting (e.g., owned vs. third-party actions)
- **Node shapes**:
  - `[ ]` rectangle (default) - services, components
  - `([ ])` stadium - entry/exit points
  - `[( )]` cylinder - databases, storage
  - `{ }` diamond - decision points
  - `(( ))` circle - events

### Synchronization

- Mermaid diagrams must stay in sync with code during refactoring
- When renaming services, update corresponding diagram nodes
- When adding/removing dependencies, update dependency graphs
- Review all `README.md` diagrams before creating PRs
