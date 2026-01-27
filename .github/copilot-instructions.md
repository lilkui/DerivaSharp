# Copilot instructions for DerivaSharp

## Big picture (how pricing flows)
- Core library code lives in [src/DerivaSharp](src/DerivaSharp) with domain split across Instruments, Models, PricingEngines, Numerics, and Time.
- Typical pricing flow: instrument (inherits `Option`) + model parameters (`IModelParameters`) → `PricingContext<TModelParameters>` → `PricingEngine<TOption,TModel>.Value()` which delegates to `CalculateValue()` overrides. See [src/DerivaSharp/Instruments/Option.cs](src/DerivaSharp/Instruments/Option.cs), [src/DerivaSharp/PricingEngines/PricingContext.cs](src/DerivaSharp/PricingEngines/PricingContext.cs), and [src/DerivaSharp/PricingEngines/PricingEngine.cs](src/DerivaSharp/PricingEngines/PricingEngine.cs).
- BSM-specific engines extend `BsmPricingEngine<TOption>` and compute Greeks/implied vol via finite-difference shifts configured in `NumericalApproximationParameters`. See [src/DerivaSharp/PricingEngines/BsmPricingEngine.cs](src/DerivaSharp/PricingEngines/BsmPricingEngine.cs) and [src/DerivaSharp/PricingEngines/NumericalApproximationParameters.cs](src/DerivaSharp/PricingEngines/NumericalApproximationParameters.cs).
- Finite-difference engines inherit from `FiniteDifference1DPricingEngine<TOption>` and build PDE grids + tridiagonal solves using `FiniteDifferenceScheme`, `TridiagonalMatrix`, and `LinearInterpolation`. See [src/DerivaSharp/PricingEngines/FiniteDifference1DPricingEngine.cs](src/DerivaSharp/PricingEngines/FiniteDifference1DPricingEngine.cs) and [src/DerivaSharp/Numerics](src/DerivaSharp/Numerics).
- Monte Carlo paths are generated with TorchSharp tensors: `RandomNumberSource` (antithetic normals) + `PathGenerator.Generate()`; device selection goes through `TorchUtils.GetDevice()`. See [src/DerivaSharp/PricingEngines/RandomNumberSource.cs](src/DerivaSharp/PricingEngines/RandomNumberSource.cs), [src/DerivaSharp/PricingEngines/PathGenerator.cs](src/DerivaSharp/PricingEngines/PathGenerator.cs), and [src/DerivaSharp/PricingEngines/TorchUtils.cs](src/DerivaSharp/PricingEngines/TorchUtils.cs).

## Project conventions (follow existing patterns)
- Value-like types are modeled as `record`/`record struct` with init-only properties (e.g., `Option`, `PricingContext<TModelParameters>`, `BsmModelParameters`).
- Validate inputs with `Guard` and throw via `ThrowHelper` using centralized constants in [src/DerivaSharp/ExceptionMessages.cs](src/DerivaSharp/ExceptionMessages.cs).
- Dates are represented with `DateOnly` (valuation/effective/expiration) throughout instruments and engines.

## Developer workflows
- .NET 10 SDK is required (see [README.md](README.md)).
- Unit tests are in [src/DerivaSharp.Tests](src/DerivaSharp.Tests) and use xUnit; run with `dotnet test` against the test project.
- Benchmarks live in [src/DerivaSharp.Benchmarks](src/DerivaSharp.Benchmarks) using BenchmarkDotNet; run the benchmarks project in Release when measuring performance.
- Jupyter examples are in [notebooks](notebooks) and use Python.NET; build binaries first (README suggests `dotnet publish -c Release -r win-x64`).

## Dependencies & integration points
- MathNet.Numerics is used for grids, interpolation, and root finding in pricing engines.
- TorchSharp with libtorch CUDA runtimes enables GPU Monte Carlo; code paths choose CPU/CUDA at runtime.
