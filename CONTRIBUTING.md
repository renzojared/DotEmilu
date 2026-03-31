# Contributing to DotEmilu

Thank you for your interest in contributing! This document provides guidelines
and information about contributing to this project.

## Getting Started

### Prerequisites

- [.NET 10 SDK](https://dot.net/download) or later
- Git

### Building the project

```shell
git clone https://github.com/renzojared/DotEmilu.git
cd DotEmilu
dotnet restore DotEmilu.slnx
dotnet build DotEmilu.slnx -c Release
```

> **NuGet lock files** — Each project has a committed `packages.lock.json` that
> freezes the exact version of every dependency (including transitive ones).
> CI runs `dotnet restore --locked-mode`, which fails if the lock file is out of
> sync with the project files.
>
> **When you change dependencies** (edit `Directory.Packages.props` or a `.csproj`),
> run `dotnet restore DotEmilu.slnx` locally — this updates the affected
> `packages.lock.json` files automatically. Commit both the props/csproj change
> **and** the regenerated lock files together, otherwise CI will fail.

### Running tests

```shell
dotnet test --solution DotEmilu.slnx -c Release
```

## How to Contribute

### Reporting Bugs

Use the [Bug Report issue template](https://github.com/renzojared/DotEmilu/issues/new?template=bug_report.yml).
Include the package name, version, .NET version, and minimal reproduction steps.

### Suggesting Features

Use the [Feature Request issue template](https://github.com/renzojared/DotEmilu/issues/new?template=feature_request.yml).
Describe the problem you want to solve and the API you have in mind.

### Submitting Pull Requests

1. **Fork** the repository and create your branch from `main`
2. Make your changes following the existing code style
3. Add or update tests for your changes
4. Ensure all tests pass and the build produces **zero warnings**
5. Fill in the [PR template](.github/PULL_REQUEST_TEMPLATE.md)
6. Submit the PR targeting `main`

### Code Style

This project enforces code style at build time:

- `TreatWarningsAsErrors` is enabled
- `EnforceCodeStyleInBuild` is enabled
- `AnalysisMode` is set to `Recommended`

Your code must compile with zero warnings. Use the `.editorconfig` and analyzer
settings already in the repository.

### Commit Messages

We follow [Conventional Commits](https://www.conventionalcommits.org/):

```
feat: add batch handler support
fix: null reference in presenter pipeline
docs: update getting started guide
chore(build): update EF Core to 10.0.5
test: add integration tests for AspNetCore endpoints
```

## Project Structure

```
src/
├── DotEmilu.Abstractions/    # Core interfaces, base entities, contracts
├── DotEmilu/                 # Handler pipeline, validation, DI registration
├── DotEmilu.AspNetCore/      # Minimal API bridges, Problem Details, presenters
└── DotEmilu.EntityFrameworkCore/  # EF Core interceptors, configurations, pagination

tests/
├── DotEmilu.UnitTests/
└── DotEmilu.IntegrationTests/

samples/
├── DotEmilu.Samples.ConsoleApp/
├── DotEmilu.Samples.Domain/
├── DotEmilu.Samples.EntityFrameworkCore/
└── DotEmilu.Samples.FullApp/
```

## Versioning

This project uses [MinVer](https://github.com/adamralph/minver) for automatic
semantic versioning from Git tags. You do not need to set version numbers
manually. See [RELEASE.md](.github/RELEASE.md) for details.

## License

By contributing, you agree that your contributions will be licensed under the
[MIT License](LICENSE).
