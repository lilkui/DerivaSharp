using System;
using System.Linq;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using DerivaSharp.Instruments;
using DerivaSharp.Models;
using DerivaSharp.PricingEngines;
using DerivaSharp.Time;

namespace DerivaSharp.Benchmarks;

[SimpleJob(RuntimeMoniker.Net10_0)]
[SimpleJob(RuntimeMoniker.NativeAot10_0)]
public class AutocallableBenchmarks
{
    private BsmModelParameters _modelParameters;
    private PricingContext<BsmModelParameters> _context;
    private SnowballOption _snowball;
    private DateOnly _effectiveDate;

    [GlobalSetup]
    public void Setup()
    {
        _modelParameters = new BsmModelParameters(0.3, 0.04, 0.01);
        _effectiveDate = new DateOnly(2022, 1, 5);

        _context = new PricingContext<BsmModelParameters>(_modelParameters, 100.0, _effectiveDate);

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
        FdSnowballEngine engine = new(FiniteDifferenceScheme.CrankNicolson, 1000, 1000);
        return engine.Value(_snowball, _context);
    }

    [Benchmark]
    public double McSnowball()
    {
        McSnowballEngine engine = new(500_000);
        return engine.Value(_snowball, _context);
    }

    [Benchmark]
    public double McSnowball_Cuda()
    {
        McSnowballEngine engine = new(500_000, true);
        return engine.Value(_snowball, _context);
    }
}
