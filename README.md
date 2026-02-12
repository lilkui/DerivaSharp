# DerivaSharp

![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)
![Stage: Experimental](https://img.shields.io/badge/stage-experimental-orange)

DerivaSharp is a modern, performance-oriented C# library for financial derivatives pricing. It provides a comprehensive suite of instruments and sophisticated pricing algorithms.

> [!NOTE]
> This project is currently in an experimental stage. Expect frequent breaking changes and API instability.

## Quick Start

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)

### Basic Usage

The following example demonstrates how to price a European Call option using the analytic engine:

```csharp
using DerivaSharp.Instruments;
using DerivaSharp.Models;
using DerivaSharp.PricingEngines;

DateOnly valuationDate = new(2025, 1, 6);
DateOnly expirationDate = valuationDate.AddYears(1);

// 1. Define the instrument
EuropeanOption option = new(
    OptionType.Call,
    100.0,
    valuationDate,
    expirationDate);

// 2. Set up the model parameters
BsmModelParameters modelParameters = new(0.3, 0.04, 0.01);

// 3. Create the pricing context
PricingContext<BsmModelParameters> context = new(
    modelParameters,
    assetPrice: 100.0,
    valuationDate);

// 4. Price the option
AnalyticEuropeanEngine engine = new();
double price = engine.Value(option, context);
double delta = engine.Delta(option, context);

Console.WriteLine($"Price: {price:F4}");
Console.WriteLine($"Delta: {delta:F4}");
```

### Interactive Notebooks

Explore the [notebooks/](notebooks/) folder for interactive examples. These notebooks demonstrate how to use the library within Jupyter using Python via [Python.NET](https://pythonnet.github.io/).

Before using the notebooks, build the library for your target platform to generate the required binaries. For example, on Windows:

```bash
dotnet publish -c Release -r win-x64
```

## Supported Instruments and Pricing Algorithms

| Instrument Category | Sub-category | Pricing Algorithms |
| :--- | :--- | :--- |
| **Vanilla Option** | European, American | Analytical, Finite Difference, Monte Carlo, Binomial Tree, Numerical Integration |
| **Barrier Option** | Standard | Analytical, Finite Difference |
| **Digital Option** | Cash-or-Nothing, Asset-or-Nothing, Binary Barrier | Analytical, Finite Difference, Numerical Integration |
| **Asian Option** | Geometric Average, Arithmetic Average | Analytical |
| **Autocallable Note** | Snowball, Phoenix, FCN, DCN | Finite Difference, Monte Carlo |
| **Accumulator** | KODA | Finite Difference, Monte Carlo |

## Performance Benchmarks

**Environment Specifications:**

- **CPU**: AMD Ryzen 9 5900X @ 3.70GHz
- **GPU**: NVIDIA GeForce RTX 3090
- **OS**: Windows 11 (25H2)
- **Runtime**: .NET 10.0.2

| Instrument | Pricing Algorithm | Parameters | Time (JIT) | Time (AOT) |
| :--- | :--- | :--- | ---: | ---: |
| **Vanilla European** | Analytical | - | 28.5 ns | 40.7 ns |
| | Finite Difference | 1000×1000 grid (CN) | 11.75 ms | 11.90 ms |
| | Monte Carlo | 500000 paths | 6.24 ms | 7.17 ms |
| | Binomial Tree | 1000 steps | 445 μs | 549 μs |
| | Numerical Integration | - | 333 ns | 464 ns |
| **Vanilla American** | Bjerksund-Stensland | - | 5.83 μs | 6.49 μs |
| | Finite Difference | 1000×1000 grid (CN) | 13.06 ms | 13.04 ms |
| | Monte Carlo | 100000 paths, 250 steps | 976 ms | 959 ms |
| **Snowball** | Finite Difference | 1000×1000 grid (CN) | 34.12 ms | 34.61 ms |
| | Monte Carlo | 500000 paths | 1.70 s | 1.94 s |
| | Monte Carlo | 500000 paths, GPU-accelerated | 40.92 ms | 40.75 ms |

*CN = Crank-Nicolson scheme*
