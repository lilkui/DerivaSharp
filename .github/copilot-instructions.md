# Copilot instructions for DerivaSharp

## Big picture

- Core library lives in [src/](src/) with domain types in [src/Instruments/](src/Instruments/), models in [src/Models/](src/Models/), and pricing engines in [src/PricingEngines/](src/PricingEngines/).
- Pricing engines share a common workflow: `PricingEngine<TOption, TModel>` handles argument validation + numerical Greeks, while concrete engines implement `CalculateValue(...)` (see [src/PricingEngines/PricingEngine.cs](src/PricingEngines/PricingEngine.cs)).
- BSM-specific engines extend `BsmPricingEngine<TOption>` for extra Greeks and implied vol (see [src/PricingEngines/BsmPricingEngine.cs](src/PricingEngines/BsmPricingEngine.cs)).
- Finite-difference engines extend `BsmFiniteDifferenceEngine<TOption>` and use `TridiagonalMatrix` and `LinearInterpolation` for PDE stepping and pricing interpolation (see [src/PricingEngines/BsmFiniteDifferenceEngine.cs](src/PricingEngines/BsmFiniteDifferenceEngine.cs), [src/Numerics/TridiagonalMatrix.cs](src/Numerics/TridiagonalMatrix.cs), [src/Numerics/LinearInterpolation.cs](src/Numerics/LinearInterpolation.cs)).
- Monte Carlo path generation uses TorchSharp tensors with optional CUDA; random numbers are antithetic via `RandomNumberSource` (see [src/PricingEngines/PathGenerator.cs](src/PricingEngines/PathGenerator.cs), [src/PricingEngines/RandomNumberSource.cs](src/PricingEngines/RandomNumberSource.cs), [src/PricingEngines/TorchUtils.cs](src/PricingEngines/TorchUtils.cs)).

## Conventions and patterns

- Domain entities are immutable `record`/`record struct` types and often use `DateOnly` for valuation/expiry (examples in [src/Instruments/Option.cs](src/Instruments/Option.cs) and [src/Models/BsmModelParameters.cs](src/Models/BsmModelParameters.cs)).
- `OptionType` encodes sign (`Call = 1`, `Put = -1`) and is used directly in formulas (see [src/Instruments/OptionType.cs](src/Instruments/OptionType.cs) and [src/PricingEngines/BsmCalculator.cs](src/PricingEngines/BsmCalculator.cs)).
- Guard clauses use `CommunityToolkit.Diagnostics.Guard` and errors are centralized in `ExceptionMessages` (see [src/Instruments/Option.cs](src/Instruments/Option.cs) and [src/ExceptionMessages.cs](src/ExceptionMessages.cs)).
- Pricing APIs typically accept `PricingContext<TModel>` which bundles model params, asset price, and valuation date (see [src/PricingEngines/PricingContext.cs](src/PricingEngines/PricingContext.cs)).

## Build, test, notebooks

- Target framework is .NET 10; StyleCop analyzers are enforced via [Directory.Build.props](Directory.Build.props) and package references in [src/DerivaSharp.csproj](src/DerivaSharp.csproj).
- Run unit tests with `dotnet test` on [tests/DerivaSharp.Tests.csproj](tests/DerivaSharp.Tests.csproj); tests use xUnit with shared test data classes (example: [tests/Vanilla/AnalyticEuropeanEngineTest.cs](tests/Vanilla/AnalyticEuropeanEngineTest.cs)).
- Notebooks under [notebooks/](notebooks/) use Python.NET; build the library before running them (README shows `dotnet publish -c Release -r win-x64`) in [README.md](README.md).

## External dependencies and integration points

- Math primitives use MathNet.Numerics (BSM calculators, root finding), TorchSharp for GPU-accelerated Monte Carlo, and CommunityToolkit for diagnostics/high-performance spans (see [src/DerivaSharp.csproj](src/DerivaSharp.csproj)).
- CUDA-enabled builds rely on platform-specific libtorch packages; check for GPU availability using `TorchUtils.GetDevice(...)` (see [src/PricingEngines/TorchUtils.cs](src/PricingEngines/TorchUtils.cs)).
