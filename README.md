# DerivaSharp

![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)
![Stage: Experimental](https://img.shields.io/badge/stage-experimental-orange)

DerivaSharp is a high-performance C# library for financial derivatives pricing. It provides a comprehensive suite of instruments and sophisticated pricing algorithms.

> [!WARNING]
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

Explore the [notebooks/](notebooks/) folder for interactive examples. These notebooks demonstrate how to use the library within Jupyter using Python via [Python.NET](http://pythonnet.github.io/).

Before using the notebooks, build the library for your target platform to generate the required binaries. For example, on Windows:

```bash
dotnet publish -c Release -r win-x64
```

## Supported Instruments and Pricing Algorithms

| Instrument | Pricing Algorithms |
| :--- | :--- |
| **Vanilla European Option** | Analytic, Finite Difference, Monte Carlo, Binomial Tree, Numerical Integration |
| **Vanilla American Option** | Bjerksund-Stensland Approximation, Finite Difference, Monte Carlo (Longstaff-Schwartz), Binomial Tree |
| **Barrier Option** | Analytic, Finite Difference |
| **Digital Option** | Analytic |
| **Binary Barrier Option** | Analytic, Finite Difference |
| **Autocallable Note (Snowball, Phoenix)** | Finite Difference, Monte Carlo |
| **Accumulator** | Finite Difference, Monte Carlo |

## Performance Benchmarks

**Environment Specifications:**

- **CPU**: AMD Ryzen 9 5900X @ 3.70GHz
- **GPU**: NVIDIA GeForce RTX 3090
- **OS**: Windows 11 (25H2)
- **Runtime**: .NET 10.0

| Instrument | Pricing Algorithm | Parameters | Time (JIT) | Time (AOT) |
| :--- | :--- | :--- | ---: | ---: |
| **Vanilla European** | Analytic | - | 45 ns | 54 ns |
| | Finite Difference | 1000×500 grid (CN) | 6.4 ms | 6.6 ms |
| | Monte Carlo | 1M paths | 11.8 ms | 13.7 ms |
| | Binomial Tree | 500 steps | 114 μs | 138 μs |
| | Numerical Integration | - | 99 μs | 106 μs |
| **Vanilla American** | Bjerksund-Stensland | - | 6.2 μs | 6.9 μs |
| | Finite Difference | 1000×500 grid (CN) | 7.0 ms | 7.2 ms |
| | Monte Carlo | 100K paths, 250 steps | 895 ms | 942 ms |
| **Snowball** | Finite Difference | 1000×500 grid (CN) | 12.5 ms | 13.0 ms |
| | Monte Carlo | 1M paths | 3.45 s | 3.89 s |
| | Monte Carlo | 1M paths, GPU-accelerated | 81.4 ms | 80.9 ms |

*CN = Crank-Nicolson scheme*
