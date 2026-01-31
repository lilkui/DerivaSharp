# Copilot instructions for DerivaSharp

## Project overview

- .NET 10 library for derivatives pricing. Core code lives in src/DerivaSharp with namespaces Instruments, Models, PricingEngines, Numerics, Time.
- Instruments are immutable record types with validation in constructors (CommunityToolkit.Diagnostics.Guard). See Instruments/Option.cs and Instruments/BarrierOption.cs.

## Architecture and data flow

- Pricing engines follow a template method: PricingEngine<TOption,TModel> validates inputs and calls CalculateValue, while finite-difference/MC/analytic engines override that method. See PricingEngines/PricingEngine.cs.
- BSM-specific engines extend BsmPricingEngine<TOption> to add implied vol and Greeks via shifted valuations. See PricingEngines/BsmPricingEngine.cs and PricingEngines/BsmCalculator.cs.
- Finite difference engines share a common PDE solver in BsmFiniteDifferenceEngine<TOption>; derived engines must set MinPrice/MaxPrice and implement terminal, boundary, and step conditions. See PricingEngines/BsmFiniteDifferenceEngine.cs and PricingEngines/Vanilla/FdEuropeanEngine.cs.
- Monte Carlo engines use TorchSharp tensors. RandomNumberSource generates antithetic normals (half and negated half), PathGenerator builds log-returns and paths, and TorchUtils selects CPU/CUDA with validation. See PricingEngines/RandomNumberSource.cs, PricingEngines/PathGenerator.cs, PricingEngines/TorchUtils.cs, and PricingEngines/Vanilla/McEuropeanEngine.cs.
- Trading day grids for time steps come from Time/DateUtils.cs and PricingEngines/TradingDayGridBuilder.cs.

## Conventions and patterns

- Use DateOnly for valuation/effective/expiration dates, and year fractions are computed as day count / 365.0 (see PricingEngine.GetYearsToExpiration).
- Observation intervals for barriers and calendars are represented in years (BarrierOption.ObservationInterval = days/365.0).
- Prefer Guard/ThrowHelper from CommunityToolkit.Diagnostics for argument validation and error messages via ExceptionMessages.cs.

## Build, test, and notebooks

- Build the solution: dotnet build DerivaSharp.slnx
- Run tests: dotnet test src/DerivaSharp.Tests/DerivaSharp.Tests.csproj (xUnit)
- Notebook usage requires published binaries; README suggests: dotnet publish -c Release -r win-x64

## External dependencies

- MathNet.Numerics for distribution functions and root finding (used in BsmCalculator and implied vol).
- TorchSharp + libtorch-cuda for Monte Carlo and GPU acceleration; guard CUDA availability with TorchUtils.GetDevice.
