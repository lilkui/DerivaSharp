# Copilot Instructions — DerivaSharp

## Project Overview

DerivaSharp is a .NET 10 C# library for financial derivatives pricing. It uses the Black-Scholes-Merton (BSM) framework with analytic, finite-difference (FD), Monte Carlo (MC), binomial-tree, and numerical-integration engines. GPU acceleration is provided via TorchSharp (CUDA).

## Architecture

```
src/Instruments/   – Immutable record hierarchy defining derivative contracts
src/Models/        – Market model parameters (BsmModelParameters)
src/PricingEngines/– Strategy-pattern engines: one engine per (instrument, method) pair
src/Numerics/      – Math primitives (distributions, root-finding, quadrature, tridiagonal solver)
src/Time/          – Trading calendar (SSE holidays), date utilities, trading-day grids
```

**Key generic constraint chain:** `PricingEngine<TOption, TModel> where TOption : Option where TModel : IModelParameters` → `BsmPricingEngine<TOption>` (fixes `TModel = BsmModelParameters`) → `BsmFiniteDifferenceEngine<TOption>` → `FdKiAutocallableEngine<TOption>`.

**Instruments are `record` types** (immutable, positional or nominal). Intermediate types are `abstract record`; leaf instruments are `sealed record`. Models and pricing context are `readonly record struct`. Engines are `abstract class`/`sealed class`.

**Template Method pattern** drives engines: override `CalculateValue()` for basic engines; override `SetTerminalCondition()`, `SetBoundaryConditions()`, `ApplyStepConditions()` for FD engines.

## Code Conventions

- **Explicit types** — avoid `var`; use `double`, `int`, concrete type names.
- **File-scoped namespaces** — `namespace DerivaSharp.Instruments;` (no braces).
- **Field naming** — `_camelCase` for instance fields, `s_camelCase` for static fields, `PascalCase` for constants.
- **No `this.`** — actively discouraged (SX1101).
- **Guard clauses** — use `CommunityToolkit.Diagnostics.Guard` (e.g., `Guard.IsGreaterThan(price, 0)`), not manual `if`/`throw`.
- **`OptionType` enum** — `Call = 1`, `Put = -1`; used as a sign multiplier in formulas (`z * (spot - strike)`).
- **`with` expressions** for parameter bumping in Greeks: `context with { AssetPrice = s + ds }`, `parameters with { Volatility = vol + dvol }`.
- **Target-typed `new()`** — preferred: `new(0.3, 0.04, 0.01)` instead of `new BsmModelParameters(0.3, 0.04, 0.01)`.
- **Trailing commas** required in multi-line initializers.
- **XML docs** on all public source types/members (4-space indented `///`). No docs on test code.
- **Year fractions** — `days / 365.0` (calendar days), not trading days. See `PricingEngine.GetYearsToExpiration()`.
- **`DateOnly`** exclusively — never `DateTime`.
- **`sealed`** on all leaf classes and records.
- **Using directives** outside namespace, System first, alphabetically sorted.

## Adding a New Instrument

1. Create a `sealed record` in `src/Instruments/` inheriting from the appropriate base (`StrikedTypePayoffOption`, `AutocallableNote`, etc.).
2. Add guard clauses in the constructor via `Guard.*` methods.
3. Add static factory methods for common configurations (see `SnowballOption.CreateStandardSnowball`).

## Adding a New Pricing Engine

1. Inherit from `BsmPricingEngine<TOption>` (analytic/MC) or `BsmFiniteDifferenceEngine<TOption>` (FD).
2. Override `CalculateValue()`. For FD, override the three template hooks instead.
3. For analytic engines, override individual Greek methods (e.g., `Delta()`, `Gamma()`) with closed-form implementations and add a `UseNumericalGreeks` toggle (see `AnalyticEuropeanEngine`).
4. MC engines use `PathGenerator` and `RandomNumberSource` from `PricingEngines/` with TorchSharp tensors; accept `useCuda` constructor parameter.

## Build & Test

```powershell
dotnet build           # Build all projects
dotnet test            # Run xUnit tests
dotnet run --project benchmarks -c Release   # BenchmarkDotNet benchmarks
dotnet publish -c Release -r win-x64         # AOT-compatible publish (needed for notebooks)
```

## Test Conventions

- Framework: **xUnit** with global `using Xunit;` (declared in `.csproj`).
- All test classes share namespace `DerivaSharp.Tests` regardless of subfolder.
- Naming: `{EngineClass}Test` → `MethodUnderTest_Condition_ExpectedOutcome`.
- Shared test data in static classes (e.g., `EuropeanOptionTestData`) exposing `TheoryData<...>` properties, imported via `using static`.
- Precision: `Assert.Equal(expected, actual, precision)` where `precision` is decimal digits (typically 6 for analytic, 2-4 for MC/FD).
- MC tolerance: relative `Math.Abs(expected) * 0.01` to `0.03`, with a floor via `Math.Max(floor, relative)`.
- No test base classes; constructor-based fixture setup with `_`-prefixed `readonly` fields.
