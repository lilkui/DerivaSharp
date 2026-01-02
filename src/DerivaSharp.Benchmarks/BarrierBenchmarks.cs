using System;
using BenchmarkDotNet.Attributes;
using DerivaSharp.Instruments;
using DerivaSharp.Models;
using DerivaSharp.PricingEngines;

namespace DerivaSharp.Benchmarks;

[MemoryDiagnoser]
public class BarrierBenchmarks
{
    private BsmModel _model;
    private PricingContext<BsmModel> _context;
    private BarrierOption _barrier;

    [GlobalSetup]
    public void Setup()
    {
        _model = new BsmModel(0.3, 0.04, 0.01);
        DateOnly effectiveDate = new(2025, 1, 6);
        DateOnly expirationDate = effectiveDate.AddDays(365);

        _context = new PricingContext<BsmModel>(_model, 100.0, effectiveDate);

        _barrier = new BarrierOption(
            OptionType.Call,
            BarrierType.UpAndOut,
            100.0,
            120.0,
            0.0,
            PaymentType.PayAtExpiry,
            100,
            effectiveDate,
            expirationDate);
    }

    [Benchmark]
    public double AnalyticBarrier()
    {
        var engine = new AnalyticBarrierEngine();
        return engine.Value(_barrier, _context);
    }

    [Benchmark]
    public double FdBarrier()
    {
        var engine = new FdBarrierEngine(FiniteDifferenceScheme.CrankNicolson, 800, 400);
        return engine.Value(_barrier, _context);
    }
}
