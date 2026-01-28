# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

DerivaSharp is a high-performance C# library for financial derivatives pricing built on .NET 10. It provides instruments (options, autocallables, accumulators) and sophisticated pricing algorithms (analytical, finite difference, Monte Carlo, binomial trees).

**Status**: Experimental - expect frequent breaking changes and API instability.

## Build and Test Commands

```bash
# Build the library
dotnet build

# Build for specific platform (required for notebooks)
dotnet publish -c Release -r win-x64

# Run all tests
dotnet test

# Run tests for a specific project
dotnet test src/DerivaSharp.Tests/DerivaSharp.Tests.csproj

# Run a single test class
dotnet test --filter "FullyQualifiedName~AnalyticEuropeanEngineTest"

# Run benchmarks (always use Release mode)
dotnet run -c Release --project src/DerivaSharp.Benchmarks/DerivaSharp.Benchmarks.csproj
```

## Architecture Overview

### Pricing Flow

The core pricing flow follows this pattern:

1. **Instrument** (inherits from `Option`) - defines the derivative contract
2. **Model Parameters** (implements `IModelParameters`) - defines market parameters (e.g., `BsmModelParameters` for Black-Scholes-Merton)
3. **Pricing Context** (`PricingContext<TModelParameters>`) - bundles model parameters, asset price, and valuation date
4. **Pricing Engine** (`PricingEngine<TOption, TModel>`) - computes value and Greeks

Example:
```csharp
EuropeanOption option = new(OptionType.Call, strike: 100.0, effectiveDate, expirationDate);
BsmModelParameters model = new(volatility: 0.3, riskFreeRate: 0.04, dividendYield: 0.01);
PricingContext<BsmModelParameters> context = new(model, assetPrice: 100.0, valuationDate);
AnalyticEuropeanEngine engine = new();
double price = engine.Value(option, context);
```

### Key Base Classes

- **`Option`** (src/DerivaSharp/Instruments/Option.cs) - abstract base for all instruments with `EffectiveDate` and `ExpirationDate`
- **`PricingEngine<TOption, TModel>`** (src/DerivaSharp/PricingEngines/PricingEngine.cs) - abstract base for all engines
  - `Value()` delegates to abstract `CalculateValue()`
  - Greeks computed via finite-difference shifts (configurable in `NumericalApproximationParameters`)
- **`BsmPricingEngine<TOption>`** (src/DerivaSharp/PricingEngines/BsmPricingEngine.cs) - extends `PricingEngine` for Black-Scholes-Merton models
  - Adds BSM-specific Greeks (Vega, Rho, etc.)
  - Provides implied volatility calculation

### Finite Difference Engines

All FD engines inherit from `BsmFiniteDifferenceEngine<TOption>` (src/DerivaSharp/PricingEngines/BsmFiniteDifferenceEngine.cs):

- Builds 1D PDE grids (price × time) and solves backward in time
- Supports three schemes: Explicit Euler, Implicit Euler, Crank-Nicolson
- Uses tridiagonal matrix solver (`TridiagonalMatrix` in src/DerivaSharp/Numerics/TridiagonalMatrix.cs)
- Subclasses implement:
  - `SetTerminalCondition()` - payoff at expiration
  - `SetBoundaryConditions()` - boundary values at min/max price
  - `ApplyStepConditions()` - early exercise, barriers, autocallable events
- `MapObservationSteps()` utility maps observation dates to grid time steps

### Monte Carlo Engines

MC engines use TorchSharp tensors for GPU acceleration:

- **`RandomNumberSource`** (src/DerivaSharp/PricingEngines/RandomNumberSource.cs) - generates antithetic normal variates
- **`PathGenerator`** (src/DerivaSharp/PricingEngines/PathGenerator.cs) - generates asset price paths
- **`TorchUtils.GetDevice()`** (src/DerivaSharp/PricingEngines/TorchUtils.cs) - selects CPU or CUDA device at runtime
- American options use Longstaff-Schwartz least-squares regression

### Project Structure

```
src/DerivaSharp/
├── Instruments/          # Option contracts (European, American, Barrier, Autocallable, etc.)
├── Models/               # Model parameters (BsmModelParameters, IModelParameters)
├── PricingEngines/       # Pricing algorithms organized by instrument type
│   ├── Vanilla/          # European/American option engines
│   ├── Barrier/          # Barrier option engines
│   ├── Digital/          # Digital option engines
│   ├── Autocallable/     # Snowball, Phoenix, Ki-autocallable engines
│   └── Accumulator/      # Accumulator engines
├── Numerics/             # Math utilities (interpolation, tridiagonal solver, distributions)
└── Time/                 # Date utilities and trading calendars
```

## Code Conventions

- **Value types**: Model as `record` or `record struct` with init-only properties (e.g., `Option`, `PricingContext<TModelParameters>`, `BsmModelParameters`)
- **Dates**: Use `DateOnly` throughout (valuation/effective/expiration dates)
- **Validation**: Use `Guard` from CommunityToolkit.Diagnostics for input validation
- **Exceptions**: Throw via `ThrowHelper` using centralized messages in src/DerivaSharp/ExceptionMessages.cs
- **Naming**: Engines follow pattern `{Algorithm}{Instrument}Engine` (e.g., `AnalyticEuropeanEngine`, `FdBarrierEngine`, `McSnowballEngine`)

## Testing

- Tests use xUnit with `[Theory]` and `[MemberData]` for parameterized tests
- Test data classes provide shared test cases (e.g., `EuropeanOptionTestData`)
- Test structure mirrors source: `src/DerivaSharp.Tests/{InstrumentType}/{EngineClass}Test.cs`
- Precision assertions use `Assert.Equal(expected, actual, precision)` where precision is decimal places

## Dependencies

- **MathNet.Numerics** - grids, interpolation, root finding
- **TorchSharp** with libtorch CUDA runtimes - GPU-accelerated Monte Carlo (CPU/CUDA selected at runtime)
- **CommunityToolkit.Diagnostics** - input validation (`Guard`)
- **CommunityToolkit.HighPerformance** - `Span2D<T>` for efficient 2D array access

## Jupyter Notebooks

Interactive examples in `notebooks/` use Python.NET to call C# library from Python. Build binaries first with `dotnet publish -c Release -r win-x64` (or appropriate platform).
