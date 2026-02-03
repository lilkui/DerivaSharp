# DerivaSharp Copilot Instructions

## Big picture

- DerivaSharp is a .NET 10 pricing library: instruments are immutable `sealed record` types in [src/Instruments](src/Instruments), models in [src/Models](src/Models), and engines in [src/PricingEngines](src/PricingEngines).
- Pricing engines follow a template method pattern: base `PricingEngine<TOption, TModel>` validates inputs, calls `CalculateValue()`, and computes Greeks via finite differences; BSM-specific engines extend `BsmPricingEngine<TOption>` (see [src/PricingEngines](src/PricingEngines)).
- Engine families: analytic formulas in `BsmCalculator`; finite-difference engines share `BsmFiniteDifferenceEngine<TOption>`; Monte Carlo engines use TorchSharp tensors with `RandomNumberSource` and `PathGenerator`.

## Core data flow (example)

- Create an instrument → create model parameters (`BsmModelParameters`) → create `PricingContext<TModel>` → call `engine.Value()`/`engine.Delta()` (see example in [README.md](README.md)).

## Conventions to follow

- Use `DateOnly` for all valuation/effective/expiration dates; year fractions are $\text{dayCount}/365.0$ (see `GetYearsToExpiration` in `PricingEngine`).
- Observation intervals for barriers are expressed in years (days/365.0), not days.
- Constructor validation uses `Guard`/`ThrowHelper` from CommunityToolkit.Diagnostics.
- Instruments remain immutable; add new instruments as `sealed record` types under [src/Instruments](src/Instruments).

## Engine implementation notes

- Finite-difference engines: override `MinPrice`/`MaxPrice`, terminal and boundary conditions, and PDE step logic in `BsmFiniteDifferenceEngine<TOption>`.
- Monte Carlo: select device with `TorchUtils.GetDevice(useCuda: true)` and scope tensors with `torch.NewDisposeScope()`.

## Tests and workflows

- Build: dotnet build DerivaSharp.slnx
- Tests: dotnet test tests/DerivaSharp.Tests.csproj (filter with FullyQualifiedName)
- Benchmarks: dotnet run --project benchmarks/DerivaSharp.Benchmarks.csproj -c Release
- Notebooks require a publish build: dotnet publish -c Release -r win-x64 (see [notebooks](notebooks)).
- Tests are organized by instrument category under [tests](tests) with per-instrument *TestData.cs and `[MemberData]`.

## External dependencies

- MathNet.Numerics (distributions, root finding), TorchSharp + libtorch-cuda (Monte Carlo/GPU), CommunityToolkit.Diagnostics/HighPerformance (validation, `Span2D`).
