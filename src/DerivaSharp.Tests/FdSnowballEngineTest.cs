using DerivaSharp.Instruments;
using DerivaSharp.Models;
using DerivaSharp.PricingEngines;
using DerivaSharp.Time;

namespace DerivaSharp.Tests;

public class FdSnowballEngineTest
{
    private const double DefaultTolerance = 0.0005;
    private readonly DateOnly _effectiveDate;
    private readonly DateOnly _expirationDate;
    private readonly DateOnly[] _koObsDates;
    private readonly BsmModel _model;
    private readonly PricingContext<BsmModel> _ctx;
    private readonly FdSnowballEngine _engine;

    public FdSnowballEngineTest()
    {
        _effectiveDate = new DateOnly(2022, 1, 5);
        _expirationDate = new DateOnly(2023, 1, 5);
        _koObsDates = DateUtils.GetObservationDates(_effectiveDate, _expirationDate, 3).ToArray();
        _model = new BsmModel(0.16, 0.02, 0.04);
        _ctx = new PricingContext<BsmModel>(_model, 1.0, _effectiveDate);
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
        Assert.Equal(expected, actual, DefaultTolerance);
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

        PricingContext<BsmModel> ctx = _ctx with { ValuationDate = new DateOnly(2022, 4, 6), AssetPrice = 1.05 };

        const double expected = 0.021067;
        double actual = _engine.Value(option, ctx);
        Assert.Equal(expected, actual, DefaultTolerance);
    }

    [Fact]
    public void StepDownSnowballValue_IsAccurate()
    {
        SnowballOption option = SnowballOption.CreateStepDownSnowball(
            0.0776,
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
        Assert.Equal(expected, actual, DefaultTolerance);
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
        Assert.Equal(expected, actual, DefaultTolerance);
    }

    [Fact]
    public void LossCappedSnowballValue_IsAccurate()
    {
        SnowballOption option = SnowballOption.CreateLossCappedSnowball(
            0.074,
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
        Assert.Equal(expected, actual, DefaultTolerance);
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
        Assert.Equal(expected, actual, DefaultTolerance);
    }

    [Fact]
    public void ImpliedKnockOutCouponRate_StepDownSnowball_IsAccurate()
    {
        SnowballOption template = SnowballOption.CreateStepDownSnowball(
            0.1,
            1.0,
            0.8,
            1.03,
            0.02,
            _koObsDates,
            BarrierTouchStatus.NoTouch,
            _effectiveDate,
            _expirationDate);

        const double expected = 0.0776;
        double actual = _engine.ImpliedCouponRate(template, _ctx, 0, true);
        Assert.Equal(expected, actual, DefaultTolerance);
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
        Assert.Equal(expected, actual, DefaultTolerance);
    }

    [Fact]
    public void ImpliedKnockOutCouponRate_LossCappedSnowball_IsAccurate()
    {
        SnowballOption template = SnowballOption.CreateLossCappedSnowball(
            0.1,
            1.0,
            0.8,
            1.03,
            0.8,
            _koObsDates,
            BarrierTouchStatus.NoTouch,
            _effectiveDate,
            _expirationDate);

        const double expected = 0.074;
        double actual = _engine.ImpliedCouponRate(template, _ctx, 0, true);
        Assert.Equal(expected, actual, DefaultTolerance);
    }
}
