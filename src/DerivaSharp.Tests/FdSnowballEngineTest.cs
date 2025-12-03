using DerivaSharp.Instruments;
using DerivaSharp.PricingEngines;
using DerivaSharp.Time;

namespace DerivaSharp.Tests;

public class FdSnowballEngineTest
{
    private readonly DateOnly _effectiveDate;
    private readonly DateOnly _expirationDate;
    private readonly DateOnly[] _koObsDates;
    private readonly PricingContext _ctx;
    private readonly FdSnowballEngine _engine;

    public FdSnowballEngineTest()
    {
        _effectiveDate = new DateOnly(2022, 1, 5);
        _expirationDate = new DateOnly(2023, 1, 5);
        _koObsDates = DateUtils.GetObservationDates(_effectiveDate, _expirationDate, 3).ToArray();
        _ctx = new PricingContext(1, _effectiveDate, 0.16, 0.02, 0.04);
        _engine = new FdSnowballEngine(FiniteDifferenceScheme.CrankNicolson, 1000, 500);
    }

    [Fact]
    public void StandardSnowballValue_IsAccurate()
    {
        SnowballOption option = SnowballOption.CreateStandardSnowball(
            0.0885,
            1.0,
            0.8,
            1.03,
            _koObsDates,
            BarrierTouchStatus.NoTouch,
            _effectiveDate,
            _expirationDate);

        const double expected = 0;
        double actual = _engine.Value(option, _ctx);
        AssertWithinRelativeTolerance(expected, actual);
    }

    [Fact]
    public void StandardSnowballValue_KnockOutOnObservationDate_IsAccurate()
    {
        SnowballOption option = SnowballOption.CreateStandardSnowball(
            0.0845,
            1.0,
            0.8,
            1.03,
            _koObsDates,
            BarrierTouchStatus.NoTouch,
            _effectiveDate,
            _expirationDate);

        PricingContext ctx = _ctx with { AssetPrice = 1.05, ValuationDate = new DateOnly(2022, 4, 6) };

        const double expected = 0.021067;
        double actual = _engine.Value(option, ctx);
        AssertWithinRelativeTolerance(expected, actual);
    }

    [Fact]
    public void StepDownSnowballValue_IsAccurate()
    {
        SnowballOption option = SnowballOption.CreateStepDownSnowball(
            0.0755,
            1.0,
            0.8,
            1.03,
            0.02,
            _koObsDates,
            BarrierTouchStatus.NoTouch,
            _effectiveDate,
            _expirationDate);

        const double expected = 0;
        double actual = _engine.Value(option, _ctx);
        AssertWithinRelativeTolerance(expected, actual);
    }

    [Fact]
    public void BothDownSnowballValue_IsAccurate()
    {
        SnowballOption option = SnowballOption.CreateBothDownSnowball(
            0.0957,
            0.005,
            1.0,
            0.8,
            1.03,
            0.02,
            _koObsDates,
            BarrierTouchStatus.NoTouch,
            _effectiveDate,
            _expirationDate);

        const double expected = 0;
        double actual = _engine.Value(option, _ctx);
        AssertWithinRelativeTolerance(expected, actual);
    }

    [Fact]
    public void LossCappedSnowballValue_IsAccurate()
    {
        SnowballOption option = SnowballOption.CreateLossCappedSnowball(
            0.072,
            1.0,
            0.8,
            1.03,
            0.8,
            _koObsDates,
            BarrierTouchStatus.NoTouch,
            _effectiveDate,
            _expirationDate);

        const double expected = 0;
        double actual = _engine.Value(option, _ctx);
        AssertWithinRelativeTolerance(expected, actual);
    }

    [Fact]
    public void ImpliedKnockOutCouponRate_StandardSnowball_IsAccurate()
    {
        SnowballOption template = SnowballOption.CreateStandardSnowball(
            0.1,
            1.0,
            0.8,
            1.03,
            _koObsDates,
            BarrierTouchStatus.NoTouch,
            _effectiveDate,
            _expirationDate);

        const double expected = 0.0885;
        double actual = _engine.ImpliedCouponRate(template, _ctx, 0, true);
        AssertWithinRelativeTolerance(expected, actual);
    }

    [Fact]
    public void ImpliedKnockOutCouponRate_BothDownSnowball_IsAccurate()
    {
        SnowballOption template = SnowballOption.CreateBothDownSnowball(
            0.1,
            0.005,
            1.0,
            0.8,
            1.03,
            0.02,
            _koObsDates,
            BarrierTouchStatus.NoTouch,
            _effectiveDate,
            _expirationDate);

        const double expected = 0.0957;
        double actual = _engine.ImpliedCouponRate(template, _ctx, 0, false);
        AssertWithinRelativeTolerance(expected, actual);
    }

    private static void AssertWithinRelativeTolerance(double expected, double actual, double relativeTolerance = 0.001)
    {
        double tolerance = expected == 0.0
            ? relativeTolerance
            : Math.Abs(expected) * relativeTolerance;
        double error = Math.Abs(actual - expected);
        Assert.True(error <= tolerance);
    }
}
