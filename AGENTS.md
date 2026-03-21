# AGENTS.md

## Project Overview

DerivaSharp is a C# derivatives pricing library targeting .NET 10. It implements pricing engines for a wide range of financial instruments — vanilla European/American options, barrier options, digital/binary options, Asian options, accumulators, and autocallable structured notes (snowballs, phoenix notes, knock-in autocallables).

Pricing approaches include:

- **Analytic** — closed-form Black-Scholes-Merton solutions
- **Integral** — numerical integration via Gauss-Legendre quadrature
- **Finite-difference** — PDE solvers on a trading-day grid
- **Monte Carlo** — path simulation using TorchSharp (GPU-accelerated via CUDA when available)
- **Binomial tree** — lattice methods for American options

Key dependencies: TorchSharp (tensor computation / CUDA), CommunityToolkit.Diagnostics & HighPerformance, StyleCop.Analyzers.

## Project Structure

```
src/                        Main library (DerivaSharp.csproj)
  Instruments/              Option & structured-note definitions
  Models/                   Model parameter types (BsmModelParameters)
  Numerics/                 Math utilities (root finding, interpolation, distributions)
  PricingEngines/           All pricing engine implementations
    Vanilla/                European, American, binomial tree engines
    Barrier/                Barrier option engines
    Digital/                Digital/binary option engines
    Asian/                  Asian option engines
    Autocallable/           Snowball, phoenix, KI-autocallable engines
    Accumulator/            Accumulator engines
  Time/                     Day counting, calendars (NullCalendar, SseCalendar)
tests/                      Unit tests (DerivaSharp.Tests.csproj)
benchmarks/                 BenchmarkDotNet performance benchmarks
notebooks/                  Python notebooks for validation / comparison
```

## Setup Commands

```bash
# Restore all dependencies (includes libtorch CUDA binaries — large download)
dotnet restore

# Build the entire solution
dotnet build

# Build a single project
dotnet build src/DerivaSharp.csproj
```

## Testing

Tests use **xunit v3** with `Microsoft.Testing.Platform` as the runner (configured in `global.json`). Test data is shared via static `*TestData` classes with `[MemberData]` attributes.

```bash
# Run all tests
dotnet test

# Run tests for a specific test class
dotnet test --filter "FullyQualifiedName~AnalyticEuropeanEngineTest"

# Run a single test method
dotnet test --filter "FullyQualifiedName~AnalyticEuropeanEngineTest.Value_IsAccurate"
```

### Test organisation

Tests mirror the `PricingEngines/` subdirectory layout:

| Folder          | Covers                                              |
|-----------------|-----------------------------------------------------|
| `Vanilla/`      | European & American analytic, FD, MC, binomial tree  |
| `Barrier/`      | Barrier option analytic & FD engines                 |
| `Digital/`      | Digital/binary option analytic, integral & FD engines |
| `Asian/`        | Arithmetic & geometric average Asian engines         |
| `Autocallable/` | Snowball, phoenix, ternary snowball, binary snowball  |
| `Accumulator/`  | FD accumulator engine                                |
| `Numerics/`     | Brent solver, quadrature, interpolation, distributions |

### Test conventions

- Test classes are named `{EngineName}Test` (e.g. `AnalyticEuropeanEngineTest`).
- Test methods follow the pattern `{Behaviour}_Is{Expected}` (e.g. `Value_IsAccurate`, `ImpliedVolatility_IsAccurate`).
- Assertions compare to a fixed number of decimal places (`Assert.Equal(expected, actual, precision)`).
- Test data classes (e.g. `EuropeanOptionTestData`) supply `[MemberData]` for theories.
- When adding a new pricing engine, add a corresponding test class and test-data class.

## Benchmarks

```bash
# Run all benchmarks
dotnet run --project benchmarks -c Release

# Run a specific benchmark class
dotnet run --project benchmarks -c Release -- --filter "*EuropeanEngine*"
```

## Code Style

The project enforces style via **StyleCop.Analyzers** and a shared `.editorconfig`. Key rules:

- **Indentation**: 4 spaces, no tabs.
- **Braces**: Allman style — opening brace on its own line.
- **Using directives**: Outside the namespace, sorted with `System` first.
- **`this.` qualifier**: Do not use (SA1101 disabled, SX1101 enabled).
- **`var` usage**: Prefer explicit types; avoid `var` even when the type is apparent.
- **Naming**:
  - Constants: `PascalCase`
  - Private static fields: `s_camelCase`
  - Private/internal instance fields: `_camelCase`
  - All other members: standard C# PascalCase
- **Nullability**: Nullable reference types are enabled (`<Nullable>enable</Nullable>`).
- **XML docs**: Not enforced by StyleCop (SA0001 disabled), but present on all public APIs.
- **Expression-bodied members**: Preferred where concise.
- **Records**: Instrument types are typically `sealed record` types.
- **File-scoped namespaces**: Used throughout.

## Build and CI

CI runs on GitHub Actions (`.github/workflows/ci.yml`) on pushes and PRs to `main`:

1. `dotnet restore`
2. `dotnet build --no-restore`
3. `dotnet test --no-build`

Target framework is `net10.0`. The library is AOT-compatible (`<IsAotCompatible>true</IsAotCompatible>`).

## Architecture Notes

- **`PricingEngine<TOption, TModel>`** is the abstract base for all engines. It provides numerical Greeks (delta, gamma, theta, etc.) via finite differencing. Engines override `CalculateValue`.
- **`BsmPricingEngine<TOption>`** extends this for BSM-model engines, adding volatility-based Greeks (vega, vanna, zomma, etc.).
- **`PricingContext<TModel>`** is a record carrying model parameters, asset price, valuation date, and calendar. It supports `with` expressions for bumped scenarios.
- **TorchSharp** is used for Monte Carlo path generation and some FD engines. CUDA availability is checked at runtime.
- **`ICalendar` / `DayCounter`** handle business-day conventions and year-fraction calculations.

## Common Patterns

- New instruments go in `src/Instruments/` and inherit from `Option` or a more specific base like `VanillaOption`, `BarrierOption`, etc.
- New pricing engines go in the appropriate `src/PricingEngines/{Category}/` subfolder and extend `PricingEngine<TOption, TModel>` or `BsmPricingEngine<TOption>`.
- Validation uses `CommunityToolkit.Diagnostics.Guard` (e.g. `Guard.IsGreaterThan`).
- Error messages are centralised in `ExceptionMessages.cs`.
