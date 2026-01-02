using System;
using System.Linq;
using BenchmarkDotNet.Attributes;
using DerivaSharp.Instruments;
using DerivaSharp.Models;
using DerivaSharp.PricingEngines;
using DerivaSharp.Time;

namespace DerivaSharp.Benchmarks;

[MemoryDiagnoser]
public class AutocallableBenchmarks
{
    private BsmModel _model;
    private PricingContext<BsmModel> _context;
    private SnowballOption _snowball;
    private DateOnly _effectiveDate;

    [GlobalSetup]
    public void Setup()
    {
        _model = new BsmModel(0.3, 0.04, 0.01);
        _effectiveDate = new DateOnly(2022, 1, 5);

        _context = new PricingContext<BsmModel>(_model, 100.0, _effectiveDate);

        DateOnly expirationDate = _effectiveDate.AddYears(1);
        DateOnly[] koObsDates = DateUtils.GetObservationDates(_effectiveDate, expirationDate, 3).ToArray();

        _snowball = SnowballOption.CreateStandardSnowball(
            0.085,
            100.0,
            0.8,
            1.03,
            koObsDates,
            BarrierTouchStatus.NoTouch,
            _effectiveDate,
            expirationDate);
    }

    [Benchmark]
    public double FdSnowball()
    {
        var engine = new FdSnowballEngine(FiniteDifferenceScheme.CrankNicolson, 800, 400);
        return engine.Value(_snowball, _context);
    }

    [Benchmark]
    public double McSnowball()
    {
        var engine = new McSnowballEngine(1_000_000);
        return engine.Value(_snowball, _context);
    }

    [Benchmark]
    public double McSnowball_Cuda()
    {
        var engine = new McSnowballEngine(1_000_000, true);
        return engine.Value(_snowball, _context);
    }
}
