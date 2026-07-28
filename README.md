# DerivaSharp

![.NET 10](https://img.shields.io/badge/.NET-10.0-512BD4)
[![License: MIT](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE.txt)

DerivaSharp is a modern C# library for pricing financial derivatives under the Black-Scholes-Merton model. It covers vanilla and exotic instruments with analytic, integral, finite-difference, binomial-tree, and Monte Carlo engines, including optional CUDA acceleration through TorchSharp.

## Features

- Prices European and American vanilla options, barriers, digitals, Asian options, autocallables, and accumulators.
- Offers closed-form formulas, Gauss-Legendre integration, PDE solvers, Cox-Ross-Rubinstein trees, and path simulation.
- Computes value and Greeks through a common pricing-engine API, with analytic Greeks where available and numerical Greeks otherwise.
- Runs Monte Carlo workloads on CPU by default or NVIDIA GPUs when CUDA is enabled.
- Includes calendar-aware trading-day grids, Actual/365 day counting, and Shanghai Stock Exchange holidays.
- Supports nullable reference types, Native AOT, and .NET 10.

## Supported instruments

| Category | Instruments | Pricing engines |
| --- | --- | --- |
| Vanilla | European and American calls and puts | Black-Scholes-Merton, Bjerksund-Stensland 2002, numerical integration, finite difference, Monte Carlo/Longstaff-Schwartz, binomial tree |
| Barrier | Up/down and in/out options with rebates and discrete observations | Closed form with discrete-barrier adjustment, finite difference |
| Digital | Cash-or-nothing, asset-or-nothing, and binary barrier options | Closed form, numerical integration, finite difference |
| Asian | Arithmetic- and geometric-average options | Turnbull-Wakeman approximation, geometric closed form |
| Autocallable | Snowball, Phoenix, binary snowball, and ternary snowball notes | Finite difference, Monte Carlo |
| Accumulator | Accumulator contracts with knock-out and gearing features | Finite difference, Monte Carlo |

The library also contains reusable numerical routines for root finding, quadrature, interpolation, normal distributions, and tridiagonal systems.

## Getting started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- An NVIDIA GPU and compatible CUDA driver only when GPU acceleration is required

Clone and build the repository:

```bash
git clone https://github.com/lilkui/DerivaSharp.git
cd DerivaSharp
dotnet restore
dotnet build --no-restore
```

> [!NOTE]
> Restore downloads the platform-specific LibTorch CUDA package for Windows or Linux, so the first restore can be large. CUDA is optional at runtime; Monte Carlo engines use the CPU unless `useCuda` is set to `true`.

DerivaSharp is currently consumed as a project reference. From another .NET project, add the cloned library with:

```bash
dotnet add MyApp.csproj reference ../DerivaSharp/src/DerivaSharp.csproj
```

## Quick start

The following example prices a one-year European call and returns its value and Greeks:

```csharp
using System;
using DerivaSharp.Instruments;
using DerivaSharp.Models;
using DerivaSharp.PricingEngines;
using DerivaSharp.Time;

DateOnly valuationDate = new(2025, 1, 6);
DateOnly expirationDate = valuationDate.AddYears(1);

EuropeanOption option = new(
    OptionType.Call,
    strikePrice: 100.0,
    valuationDate,
    expirationDate);

BsmModelParameters model = new(
    volatility: 0.30,
    riskFreeRate: 0.04,
    dividendYield: 0.01);

PricingContext<BsmModelParameters> context = new(
    model,
    AssetPrice: 100.0,
    valuationDate,
    NullCalendar.Shared);

AnalyticEuropeanEngine engine = new();
PricingResult result = engine.ValueAndGreeks(option, context);

Console.WriteLine($"Value: {result.Value:F4}");
Console.WriteLine($"Delta: {result.Delta:F4}");
Console.WriteLine($"Gamma: {result.Gamma:F4}");
Console.WriteLine($"Vega:  {result.Vega:F4}");
```

Model inputs such as volatility, rates, yields, barrier levels, and coupon rates use decimal form: `0.30` means 30%.

Every BSM engine exposes `Value`, `Delta`, `Gamma`, `Speed`, `Theta`, `Charm`, `Color`, `Vega`, `Vanna`, `Zomma`, `Rho`, and `ValueAndGreeks`. Engines can also evaluate value, delta, and gamma across an array of spot prices with `Values`, `Deltas`, and `Gammas`.

### GPU-accelerated Monte Carlo

Pass `useCuda: true` to a Monte Carlo engine to run path generation on the GPU:

```csharp
McEuropeanEngine engine = new(
    pathCount: 500_000,
    stepCount: 252,
    useCuda: true,
    seed: 42);
```

Use a fixed seed when reproducibility matters. Leave `useCuda` at its default value of `false` for CPU execution.

## Notebooks

The [`notebooks`](notebooks/) directory contains Python.NET notebooks for European options, snowballs, and accumulators. They provide interactive validation and plots using NumPy, pandas, and Matplotlib.

The notebooks currently expect a Windows x64 publish output. Build it before opening them with a Python 3.10-3.13 Jupyter environment:

```bash
dotnet publish src/DerivaSharp.csproj -c Release -r win-x64
```

## Development

Run the same restore, build, and test sequence used by CI:

```bash
dotnet restore
dotnet build --no-restore
dotnet test --no-build
```

Run a focused test class:

```bash
dotnet test --filter "FullyQualifiedName~AnalyticEuropeanEngineTest"
```

Run all benchmarks or select a benchmark class:

```bash
dotnet run --project benchmarks -c Release
dotnet run --project benchmarks -c Release -- --filter "*EuropeanEngine*"
```

BenchmarkDotNet exercises both the .NET 10 JIT and Native AOT jobs. CPU and CUDA results depend heavily on grid sizes, path counts, hardware, and driver versions.

## Project structure

```text
src/
  Instruments/       Contract definitions and payoff terms
  Models/            Black-Scholes-Merton model parameters
  Numerics/          Solvers, quadrature, interpolation, and distributions
  PricingEngines/    Analytic, integral, FD, tree, and Monte Carlo engines
  Time/              Day counting and trading calendars
tests/                xUnit v3 tests mirroring the engine categories
benchmarks/           BenchmarkDotNet JIT and Native AOT benchmarks
notebooks/            Python.NET validation notebooks
```
