using System;
using BenchmarkDotNet.Attributes;
using DerivaSharp.Instruments;
using DerivaSharp.Models;
using DerivaSharp.PricingEngines;

namespace DerivaSharp.Benchmarks;

[MemoryDiagnoser]
public class VanillaBenchmarks
{
    private BsmModel _model;
    private PricingContext<BsmModel> _context;
    private EuropeanOption _european;
    private AmericanOption _american;

    [GlobalSetup]
    public void Setup()
    {
        _model = new BsmModel(0.3, 0.04, 0.01);
        DateOnly effectiveDate = new(2025, 1, 6);
        DateOnly expirationDate = effectiveDate.AddDays(365);

        _context = new PricingContext<BsmModel>(_model, 100.0, effectiveDate);

        _european = new EuropeanOption(OptionType.Call, 100.0, effectiveDate, expirationDate);
        _american = new AmericanOption(OptionType.Call, 100.0, effectiveDate, expirationDate);
    }

    [Benchmark]
    public double AnalyticEuropean()
    {
        var engine = new AnalyticEuropeanEngine();
        return engine.Value(_european, _context);
    }

    [Benchmark]
    public double IntegralEuropean()
    {
        var engine = new IntegralEuropeanEngine();
        return engine.Value(_european, _context);
    }

    [Benchmark]
    public double FdEuropean()
    {
        var engine = new FdEuropeanEngine(FiniteDifferenceScheme.CrankNicolson, 800, 400);
        return engine.Value(_european, _context);
    }

    [Benchmark]
    public double BinomialTreeVanilla()
    {
        var engine = new BinomialTreeVanillaEngine(500);
        return engine.Value(_european, _context);
    }

    [Benchmark]
    public double McEuropean()
    {
        var engine = new McEuropeanEngine(1_000_000, 2);
        return engine.Value(_european, _context);
    }

    [Benchmark]
    public double BjerksundStenslandAmerican()
    {
        var engine = new BjerksundStenslandAmericanEngine();
        return engine.Value(_american, _context);
    }

    [Benchmark]
    public double FdAmerican()
    {
        var engine = new FdAmericanEngine(FiniteDifferenceScheme.CrankNicolson, 800, 400);
        return engine.Value(_american, _context);
    }

    [Benchmark]
    public double McAmerican()
    {
        var engine = new McAmericanEngine(100_000, 250);
        return engine.Value(_american, _context);
    }
}
