---
description: '.NET solution and build structure, central package management, solution format and SDK pinning.'
applyTo: '**/*.csproj,**/*.slnx,**/Directory.Build.props,**/Directory.Packages.props,**/global.json'
---

# .NET Solution and Build Structure

## Central Build Configuration

- Keep shared MSBuild properties in the root `Directory.Build.props`, including namespace, language version, nullable reference types, implicit usings, warning policy, package metadata, and deterministic CI builds.
- Keep individual project files focused on project-specific properties and references.
- Centralize warning suppressions in `Directory.Build.props`, with a comment naming each suppressed diagnostic.
- Use conditional property groups for cross-cutting project categories, such as test documentation generation and packability.

## Central Package Management

- Define every NuGet package version in the root `Directory.Packages.props` and keep `ManagePackageVersionsCentrally` enabled.
- Use versionless `PackageReference` items in project files.

## Solution Format

- Use the modern XML `.slnx` solution format.
- Keep Debug and Release variants named `CasCap.Api.GooglePhotos.Debug.slnx` and `CasCap.Api.GooglePhotos.Release.slnx`.
- The Debug solution uses local `ProjectReference` items for CasCap.Common projects; the Release solution uses published `PackageReference` items.
- Prefer the Debug solution for local builds.

## SDK Pinning

- Pin the .NET SDK version and roll-forward policy in the root `global.json` for reproducible local and CI builds.