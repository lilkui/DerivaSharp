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
| **Vanilla European Option** | Analytical, Finite Difference, Monte Carlo, Binomial Tree, Numerical Integration |
| **Vanilla American Option** | Bjerksund-Stensland Approximation, Finite Difference, Monte Carlo (Longstaff-Schwartz), Binomial Tree |
| **Barrier Option** | Analytical, Finite Difference |
| **Digital Option** | Analytical |
| **Binary Barrier Option** | Analytical, Finite Difference |
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
| **Vanilla European** | Analytical | - | 45.6 ns | 54.5 ns |
| | Finite Difference | 1000×1000 grid (CN) | 12.0 ms | 11.8 ms |
| | Monte Carlo | 500000 paths | 5.9 ms | 7.0 ms |
| | Binomial Tree | 1000 steps | 477 μs | 543 μs |
| | Numerical Integration | - | 104 μs | 106 μs |
| **Vanilla American** | Bjerksund-Stensland | - | 6.2 μs | 6.9 μs |
| | Finite Difference | 1000×1000 grid (CN) | 12.9 ms | 13.0 ms |
| | Monte Carlo | 100000 paths, 250 steps | 900 ms | 965 ms |
| **Snowball** | Finite Difference | 1000×1000 grid (CN) | 26.1 ms | 25.4 ms |
| | Monte Carlo | 500000 paths | 1.71 s | 1.95 s |
| | Monte Carlo | 500000 paths, GPU-accelerated | 40.8 ms | 40.9 ms |

*CN = Crank-Nicolson scheme*
