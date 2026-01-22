using DerivaSharp.Instruments;
using DerivaSharp.Models;
using DerivaSharp.PricingEngines;

namespace DerivaSharp.Tests;

public class FdTernarySnowballEngineTest
{
    private const double DefaultTolerance = 0.0001;
    private readonly DateOnly _effectiveDate;
    private readonly DateOnly _expirationDate;
    private readonly PricingContext<BsmModelParameters> _ctx;
    private readonly FdTernarySnowballEngine _engine;

    public FdTernarySnowballEngineTest()
    {
        _effectiveDate = new DateOnly(2022, 1, 5);
        _expirationDate = new DateOnly(2023, 1, 5);
        BsmModelParameters parameters = new(0.16, 0.02, 0.04);
        _ctx = new PricingContext<BsmModelParameters>(parameters, 1.0, _effectiveDate);
        _engine = new FdTernarySnowballEngine(FiniteDifferenceScheme.CrankNicolson, 1000, 500);
    }

    [Fact]
    public void TernarySnowballValue_KnockOutOnObservationDate_IsAccurate()
    {
        const double koRate = 0.067;
        const double maturityRate = 0.04;
        const double minimalRate = 0.01;
        TernarySnowballOption option = TernarySnowballOption.Create(
            koRate,
            maturityRate,
            minimalRate,
            1.0,
            0.8,
            1.03,
            BarrierTouchStatus.NoTouch,
            _effectiveDate,
            _expirationDate);
        const double expected = 0.02;
        double actual = _engine.Value(option, _ctx);
        Assert.Equal(expected, actual, DefaultTolerance);
    }
}
