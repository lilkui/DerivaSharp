# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build Commands

```bash
# Build the solution
dotnet build DerivaSharp.slnx

# Run all tests
dotnet test src/DerivaSharp.Tests/DerivaSharp.Tests.csproj

# Run a single test by name
dotnet test src/DerivaSharp.Tests/DerivaSharp.Tests.csproj --filter "FullyQualifiedName~TestMethodName"

# Run benchmarks
dotnet run --project src/DerivaSharp.Benchmarks/DerivaSharp.Benchmarks.csproj -c Release

# Publish for notebooks (platform-specific)
dotnet publish -c Release -r win-x64
```

## Architecture

### Core Design Pattern

Pricing engines follow a template method pattern: `PricingEngine<TOption,TModel>` validates inputs and calls `CalculateValue`, while derived engines (finite-difference, Monte Carlo, analytic) override that method.

### Key Namespaces

- **Instruments**: Immutable record types with constructor validation via `CommunityToolkit.Diagnostics.Guard`
- **Models**: Pricing model parameters (e.g., `BsmModelParameters` for Black-Scholes-Merton)
- **PricingEngines**: Algorithm implementations organized by instrument type
- **Numerics**: Numerical utilities and solvers
- **Time**: Date utilities and trading day calculations

### Engine Hierarchies

- **BSM engines** extend `BsmPricingEngine<TOption>` to add implied vol and Greeks via shifted valuations. See `BsmCalculator.cs` for shared BSM pricing formulas.
- **Finite difference engines** share a common PDE solver in `BsmFiniteDifferenceEngine<TOption>`; derived engines set `MinPrice`/`MaxPrice` and implement terminal, boundary, and step conditions.
- **Monte Carlo engines** use TorchSharp tensors. `RandomNumberSource` generates antithetic normals, `PathGenerator` builds log-returns and paths, and `TorchUtils` selects CPU/CUDA.

### Conventions

- Use `DateOnly` for valuation/effective/expiration dates
- Year fractions computed as day count / 365.0 (see `PricingEngine.GetYearsToExpiration`)
- Observation intervals for barriers represented in years (days/365.0)
- Use `Guard`/`ThrowHelper` from CommunityToolkit.Diagnostics for argument validation

### External Dependencies

- **MathNet.Numerics**: Distribution functions and root finding (used in BsmCalculator and implied vol)
- **TorchSharp + libtorch-cuda**: Monte Carlo and GPU acceleration; guard CUDA availability with `TorchUtils.GetDevice`
