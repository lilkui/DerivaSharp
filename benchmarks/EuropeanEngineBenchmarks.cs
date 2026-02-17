using System;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using DerivaSharp.Instruments;
using DerivaSharp.Models;
using DerivaSharp.PricingEngines;

namespace DerivaSharp.Benchmarks;

[SimpleJob(RuntimeMoniker.Net10_0)]
[SimpleJob(RuntimeMoniker.NativeAot10_0)]
public class EuropeanEngineBenchmarks
{
    private BsmModelParameters _modelParameters;
    private PricingContext<BsmModelParameters> _context;
    private EuropeanOption _european;

    [GlobalSetup]
    public void Setup()
    {
        _modelParameters = new BsmModelParameters(0.3, 0.04, 0.01);
        DateOnly effectiveDate = new(2025, 1, 6);
        DateOnly expirationDate = effectiveDate.AddDays(365);
        _context = new PricingContext<BsmModelParameters>(_modelParameters, 100.0, effectiveDate);
        _european = new EuropeanOption(OptionType.Call, 100.0, effectiveDate, expirationDate);
    }

    [Benchmark]
    public double AnalyticEuropean()
    {
        AnalyticEuropeanEngine engine = new();
        return engine.Value(_european, _context);
    }

    [Benchmark]
    public double IntegralEuropean()
    {
        IntegralEuropeanEngine engine = new();
        return engine.Value(_european, _context);
    }

    [Benchmark]
    public double FdEuropean()
    {
        FdEuropeanEngine engine = new(FiniteDifferenceScheme.CrankNicolson, 1000, 1000);
        return engine.Value(_european, _context);
    }

    [Benchmark]
    public double BinomialTreeVanilla()
    {
        BinomialTreeVanillaEngine engine = new(1000);
        return engine.Value(_european, _context);
    }

    [Benchmark]
    public double McEuropean()
    {
        McEuropeanEngine engine = new(500_000, 2);
        return engine.Value(_european, _context);
    }
}
