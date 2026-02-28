# Repository Guidelines

## Project Structure & Module Organization

- `src/` contains the library code, organized by domain (`Instruments`, `PricingEngines`, `Models`, `Numerics`, `Time`).
- `tests/` contains xUnit v3 coverage, grouped by product area (`Vanilla`, `Digital`, `Barrier`, `Autocallable`, `Asian`, `Accumulator`, `Numerics`).
- `benchmarks/` hosts BenchmarkDotNet performance suites.
- `notebooks/` includes Jupyter examples that consume published binaries.
- Root configuration files (`.editorconfig`, `stylecop.json`, `Directory.Build.props`, `StyleCopAnalyzers.ruleset`) define shared coding and analyzer rules.

## Build, Test, and Development Commands

- `dotnet restore` - restore NuGet dependencies for all projects.
- `dotnet build DerivaSharp.slnx -c Release` - build library, tests, and benchmarks with analyzers.
- `dotnet test --project tests/DerivaSharp.Tests.csproj -v minimal` - run the full test suite.
- `dotnet run --project benchmarks/DerivaSharp.Benchmarks.csproj -c Release` - execute performance benchmarks.
- `dotnet publish src/DerivaSharp.csproj -c Release -r win-x64` - produce binaries used by notebooks.

## Coding Style & Naming Conventions

- Use 4-space indentation, UTF-8, and Unix-final newline rules from `.editorconfig`.
- Follow C# Allman brace style and keep `using` directives outside namespaces.
- Naming: PascalCase for public APIs and constants, `_camelCase` for private/internal fields, and `s_camelCase` for private/internal static fields.
- Prefer explicit types over `var` unless the type is obvious from the right-hand side.
- Keep nullable warnings clean (`<Nullable>enable</Nullable>`) and address StyleCop analyzer findings before merging.

## Testing Guidelines

- Write tests with xUnit v3 (`[Fact]`, `[Theory]`, `[MemberData]`).
- Name test files `*Test.cs` and test methods with behavior-focused names (e.g., `Value_IsAccurate`).
- For pricing logic, include explicit numeric tolerance checks (`precision` constants) and boundary-date scenarios.
- Add tests in the matching domain folder whenever adding or changing an instrument or engine.

## Commit & Pull Request Guidelines

- Use concise, imperative commit subjects (e.g., `Add NullCalendar`, `Refactor day counting utility`).
- Keep commits scoped to one logical change and include tests with code changes.
- PRs should include: purpose, affected modules, validation steps (`dotnet build`, `dotnet test`), and linked issues if applicable.
- For performance-sensitive changes, include benchmark notes from `benchmarks/` in the PR description.
