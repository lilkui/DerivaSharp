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
    private readonly PricingContext<BsmModelParameters> _ctx;
    private readonly FdSnowballEngine _engine;

    public FdSnowballEngineTest()
    {
        _effectiveDate = new DateOnly(2022, 1, 5);
        _expirationDate = new DateOnly(2023, 1, 5);
        _koObsDates = DateUtils.GetObservationDates(_effectiveDate, _expirationDate, 3).ToArray();
        BsmModelParameters parameters = new(0.16, 0.02, 0.04);
        _ctx = new PricingContext<BsmModelParameters>(parameters, 1.0, _effectiveDate);
        _engine = new FdSnowballEngine(FiniteDifferenceScheme.CrankNicolson, 1000, 1000);
    }

    [Fact]
    public void StandardSnowballValue_IsAccurate()
    {
        SnowballOption option = SnowballOption.CreateStandardSnowball(
            0.0892,
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

        PricingContext<BsmModelParameters> ctx = _ctx with { ValuationDate = new DateOnly(2022, 4, 6), AssetPrice = 1.05 };

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
    public void DualCouponSnowballValue_IsAccurate()
    {
        SnowballOption option = SnowballOption.CreateDualCouponSnowball(
            0.129,
            0.04,
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
    public void ParachuteSnowballValue_IsAccurate()
    {
        SnowballOption option = SnowballOption.CreateParachuteSnowball(
            0.0880,
            1.0,
            0.8,
            1.03,
            0.95,
            _koObsDates,
            BarrierTouchStatus.NoTouch,
            _effectiveDate,
            _expirationDate);

        const double expected = 0;
        double actual = _engine.Value(option, _ctx);
        Assert.Equal(expected, actual, DefaultTolerance);
    }

    [Fact]
    public void OtmSnowballValue_IsAccurate()
    {
        SnowballOption option = SnowballOption.CreateOtmSnowball(
            0.11,
            1.0,
            0.8,
            1.03,
            1.05,
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
            0.0746,
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
    public void EuropeanSnowballValue_IsAccurate()
    {
        SnowballOption option = SnowballOption.CreateEuropeanSnowball(
            0.053,
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

        const double expected = 0.0892;
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
    public void ImpliedKnockOutCouponRate_DualCouponSnowball_IsAccurate()
    {
        SnowballOption template = SnowballOption.CreateDualCouponSnowball(
            0.1,
            0.04,
            1.0,
            0.8,
            1.03,
            _koObsDates,
            BarrierTouchStatus.NoTouch,
            _effectiveDate,
            _expirationDate);

        const double expected = 0.129;
        double actual = _engine.ImpliedCouponRate(template, _ctx, 0, false);
        Assert.Equal(expected, actual, DefaultTolerance);
    }

    [Fact]
    public void ImpliedKnockOutCouponRate_ParachuteSnowball_IsAccurate()
    {
        SnowballOption template = SnowballOption.CreateParachuteSnowball(
            0.1,
            1.0,
            0.8,
            1.03,
            0.95,
            _koObsDates,
            BarrierTouchStatus.NoTouch,
            _effectiveDate,
            _expirationDate);

        const double expected = 0.0880;
        double actual = _engine.ImpliedCouponRate(template, _ctx, 0, true);
        Assert.Equal(expected, actual, DefaultTolerance);
    }

    [Fact]
    public void ImpliedKnockOutCouponRate_OtmSnowball_IsAccurate()
    {
        SnowballOption template = SnowballOption.CreateOtmSnowball(
            0.1,
            1.0,
            0.8,
            1.03,
            1.05,
            _koObsDates,
            BarrierTouchStatus.NoTouch,
            _effectiveDate,
            _expirationDate);

        const double expected = 0.110;
        double actual = _engine.ImpliedCouponRate(template, _ctx, 0, true);
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

        const double expected = 0.0746;
        double actual = _engine.ImpliedCouponRate(template, _ctx, 0, true);
        Assert.Equal(expected, actual, DefaultTolerance);
    }

    [Fact]
    public void ImpliedCouponRate_EuropeanSnowball_IsAccurate()
    {
        SnowballOption template = SnowballOption.CreateEuropeanSnowball(
            0.1,
            1.0,
            0.8,
            1.03,
            _koObsDates,
            BarrierTouchStatus.NoTouch,
            _effectiveDate,
            _expirationDate);

        const double expected = 0.053;
        double actual = _engine.ImpliedCouponRate(template, _ctx, 0, true);
        Assert.Equal(expected, actual, DefaultTolerance);
    }
}
