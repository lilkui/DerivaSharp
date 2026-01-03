using DerivaSharp.Instruments;
using DerivaSharp.Models;
using DerivaSharp.PricingEngines;
using DerivaSharp.Time;

namespace DerivaSharp.Tests;

public class McSnowballEngineTest
{
    private const double DefaultTolerance = 0.003;
    private readonly DateOnly _effectiveDate;
    private readonly DateOnly _expirationDate;
    private readonly DateOnly[] _koObsDates;
    private readonly PricingContext<BsmModel> _ctx;
    private readonly McSnowballEngine _engine;

    public McSnowballEngineTest()
    {
        _effectiveDate = new DateOnly(2022, 1, 5);
        _expirationDate = new DateOnly(2023, 1, 5);
        _koObsDates = DateUtils.GetObservationDates(_effectiveDate, _expirationDate, 3).ToArray();
        BsmModel model = new(0.16, 0.02, 0.04);
        _ctx = new PricingContext<BsmModel>(model, 1.0, _effectiveDate);
        _engine = new McSnowballEngine(100000);
    }

    [Fact]
    public void StandardSnowballValue_IsAccurate()
    {
        SnowballOption option = SnowballOption.CreateStandardSnowball(
            0.085,
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
            0.085,
            1.0,
            0.8,
            1.03,
            _koObsDates,
            BarrierTouchStatus.NoTouch,
            _effectiveDate,
            _expirationDate);

        PricingContext<BsmModel> ctx = _ctx with { ValuationDate = new DateOnly(2022, 4, 6), AssetPrice = 1.05 };

        const double expected = 0.021192;
        double actual = _engine.Value(option, ctx);
        Assert.Equal(expected, actual, 6);
    }

    [Fact]
    public void StepDownSnowballValue_IsAccurate()
    {
        SnowballOption option = SnowballOption.CreateStepDownSnowball(
            0.076,
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
            0.094,
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
            0.125,
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
            0.084,
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
            0.105,
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
            0.071,
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
    public void ImpliedCouponRate_StandardSnowball_IsAccurate()
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

        const double expected = 0.086;
        double actual = _engine.ImpliedCouponRate(template, _ctx, 0, true);
        Assert.Equal(expected, actual, DefaultTolerance);
    }

    [Fact]
    public void ImpliedCouponRate_StepDownSnowball_IsAccurate()
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

        const double expected = 0.076;
        double actual = _engine.ImpliedCouponRate(template, _ctx, 0, true);
        Assert.Equal(expected, actual, DefaultTolerance);
    }

    [Fact]
    public void ImpliedCouponRate_BothDownSnowball_IsAccurate()
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

        const double expected = 0.094;
        double actual = _engine.ImpliedCouponRate(template, _ctx, 0, false);
        Assert.Equal(expected, actual, DefaultTolerance);
    }

    [Fact]
    public void ImpliedCouponRate_DualCouponSnowball_IsAccurate()
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

        const double expected = 0.125;
        double actual = _engine.ImpliedCouponRate(template, _ctx, 0, false);
        Assert.Equal(expected, actual, DefaultTolerance);
    }

    [Fact]
    public void ImpliedCouponRate_ParachuteSnowball_IsAccurate()
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

        const double expected = 0.084;
        double actual = _engine.ImpliedCouponRate(template, _ctx, 0, true);
        Assert.Equal(expected, actual, DefaultTolerance);
    }

    [Fact]
    public void ImpliedCouponRate_OtmSnowball_IsAccurate()
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

        const double expected = 0.105;
        double actual = _engine.ImpliedCouponRate(template, _ctx, 0, true);
        Assert.Equal(expected, actual, DefaultTolerance);
    }

    [Fact]
    public void ImpliedCouponRate_LossCappedSnowball_IsAccurate()
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

        const double expected = 0.071;
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
