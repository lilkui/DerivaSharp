using DerivaSharp.Instruments;
using DerivaSharp.Models;
using DerivaSharp.PricingEngines;

namespace DerivaSharp.Tests;

public class FdBinarySnowballEngineTest
{
    private const double DefaultTolerance = 0.0001;
    private readonly DateOnly _effectiveDate;
    private readonly DateOnly _expirationDate;
    private readonly PricingContext<BsmModelParameters> _ctx;
    private readonly FdBinarySnowballEngine _engine;

    public FdBinarySnowballEngineTest()
    {
        _effectiveDate = new DateOnly(2022, 1, 5);
        _expirationDate = new DateOnly(2023, 1, 5);
        BsmModelParameters parameters = new(0.16, 0.02, 0.04);
        _ctx = new PricingContext<BsmModelParameters>(parameters, 1.0, _effectiveDate);
        _engine = new FdBinarySnowballEngine(FiniteDifferenceScheme.CrankNicolson, 1000, 1000);
    }

    [Fact]
    public void BinarySnowballValue_KnockOutOnObservationDate_IsAccurate()
    {
        const double koRate = 0.094;
        const double maturityRate = 0.01;
        BinarySnowballOption option = BinarySnowballOption.Create(
            koRate,
            maturityRate,
            1.0,
            1.03,
            BarrierTouchStatus.NoTouch,
            _effectiveDate,
            _expirationDate);
        const double expected = 0.02;
        double actual = _engine.Value(option, _ctx);
        Assert.Equal(expected, actual, DefaultTolerance);
    }
}
