# DerivaSharp

[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](https://opensource.org/licenses/MIT)

DerivaSharp is a high-performance C# library for financial derivatives pricing, providing a comprehensive suite of instruments and sophisticated pricing algorithms.

## Quick Start

### Usage Example

Here is a simple example of pricing a European Call option using the analytic engine:

```csharp
using DerivaSharp.Instruments;
using DerivaSharp.Models;
using DerivaSharp.PricingEngines;

// 1. Define the instrument
var option = new EuropeanOption(
    OptionType.Call,
    100.0,
    new DateOnly(2024, 1, 1),
    new DateOnly(2025, 1, 1));

// 2. Set up the model
var model = new BsmModel(0.3, 0.04, 0.01);

// 3. Create the pricing context
var context = new PricingContext<BsmModel>(
    model,
    100.0,
    new DateOnly(2024, 1, 1));

// 4. Price the option
var engine = new AnalyticEuropeanEngine();
double price = engine.Value(option, context);
double delta = engine.Delta(option, context);

Console.WriteLine($"Price: {price:F4}");
Console.WriteLine($"Delta: {delta:F4}");
```

### Interactive Notebooks

Explore the [notebooks/](notebooks/) folder for interactive examples. These notebooks demonstrate how to use the library within Jupyter using Python via [Python.NET](http://pythonnet.github.io/).

## Supported Instruments and Pricing Algorithms

| Instrument | Pricing Algorithms |
| :--- | :--- |
| **Vanilla European Option** | Analytic, Finite Difference, Monte Carlo, Binomial Tree, Integral |
| **Vanilla American Option** | Bjerksund-Stensland Approximation, Finite Difference, Monte Carlo (Longstaff-Schwartz), Binomial Tree |
| **Barrier Option** | Analytic, Finite Difference |
| **Digital Option** | Analytic |
| **Binary Barrier Option** | Analytic, Finite Difference |
| **Snowball Option** | Finite Difference, Monte Carlo |
| **Phoenix Option** | Finite Difference, Monte Carlo |

## Performance Benchmarks

**System Configuration:**

- **CPU**: AMD Ryzen 9 5900X 3.70GHz
- **GPU**: NVIDIA GeForce RTX 3090
- **OS**: Windows 11 (25H2)
- **Runtime**: .NET 10.0.1

| Instrument | Pricing Algorithm | Parameters | Execution Time |
| :--- | :--- | :--- | ---: |
| **European Option** | Analytic | - | 0.000000045 s |
| | Finite Difference | 800×400 grid (CN) | 0.0038 s |
| | Monte Carlo | 1M paths | 0.012 s |
| | Binomial Tree | 500 steps | 0.00011 s |
| | Integral | - | 0.000099 s |
| **American Option** | Bjerksund-Stensland | - | 0.0000061 s |
| | Finite Difference | 800×400 grid (CN) | 0.0042 s |
| | Monte Carlo | 100K paths, 250 steps | 0.89 s |
| **Barrier Option** | Analytic | - | 0.00000026 s |
| | Finite Difference | 800×400 grid (CN) | 0.0038 s |
| **Snowball Option** | Finite Difference | 800×400 grid (CN) | 0.0078 s |
| | Monte Carlo | 1M paths | 3.4 s |
| | Monte Carlo | 1M paths, GPU-accelerated | 0.081 s |

*CN = Crank-Nicolson scheme*
