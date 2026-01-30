using DerivaSharp.Instruments;
using DerivaSharp.Models;
using DerivaSharp.PricingEngines;
using DerivaSharp.Time;

namespace DerivaSharp.Tests;

public class FdPhoenixEngineTest
{
    private const double DefaultTolerance = 0.0005;
    private readonly DateOnly _effectiveDate;
    private readonly DateOnly _expirationDate;
    private readonly DateOnly[] _obsDates;
    private readonly PricingContext<BsmModelParameters> _ctx;
    private readonly FdPhoenixEngine _engine;

    public FdPhoenixEngineTest()
    {
        _effectiveDate = new DateOnly(2022, 1, 5);
        _expirationDate = new DateOnly(2023, 1, 5);
        _obsDates = DateUtils.GetObservationDates(_effectiveDate, _expirationDate, 3).ToArray();
        BsmModelParameters parameters = new(0.16, 0.02, 0.04);
        _ctx = new PricingContext<BsmModelParameters>(parameters, 1.0, _effectiveDate);
        _engine = new FdPhoenixEngine(FiniteDifferenceScheme.CrankNicolson, 1000, 1000);
    }

    [Fact]
    public void StandardPhoenixValue_IsAccurate()
    {
        PhoenixOption option = PhoenixOption.CreateStandardPhoenix(
            0.0019,
            1.0,
            0.7,
            1.03,
            _obsDates,
            BarrierTouchStatus.NoTouch,
            _effectiveDate,
            _expirationDate);

        const double expected = 0.0;
        double actual = _engine.Value(option, _ctx);
        Assert.Equal(expected, actual, DefaultTolerance);
    }

    [Fact]
    public void FixedCouponNoteValue_IsAccurate()
    {
        PhoenixOption option = PhoenixOption.CreateFixedCouponNote(
            0.0012,
            1.0,
            0.7,
            1.03,
            _obsDates,
            BarrierTouchStatus.NoTouch,
            _effectiveDate,
            _expirationDate);

        const double expected = 0.0;
        double actual = _engine.Value(option, _ctx);
        Assert.Equal(expected, actual, DefaultTolerance);
    }

    [Fact]
    public void DigitalCouponNoteValue_IsAccurate()
    {
        PhoenixOption option = PhoenixOption.CreateDigitalCouponNote(
            0.0013,
            1.0,
            0.7,
            1.03,
            _obsDates,
            BarrierTouchStatus.NoTouch,
            _effectiveDate,
            _expirationDate);

        const double expected = 0.0;
        double actual = _engine.Value(option, _ctx);
        Assert.Equal(expected, actual, DefaultTolerance);
    }

    [Fact]
    public void ImpliedCouponRate_StandardPhoenix_IsAccurate()
    {
        PhoenixOption template = PhoenixOption.CreateStandardPhoenix(
            0.001,
            1.0,
            0.7,
            1.03,
            _obsDates,
            BarrierTouchStatus.NoTouch,
            _effectiveDate,
            _expirationDate);

        const double expected = 0.0019;
        double actual = _engine.ImpliedCouponRate(template, _ctx, 0);
        Assert.Equal(expected, actual, DefaultTolerance);
    }

    [Fact]
    public void ImpliedCouponRate_FixedCouponNote_IsAccurate()
    {
        PhoenixOption template = PhoenixOption.CreateFixedCouponNote(
            0.001,
            1.0,
            0.7,
            1.03,
            _obsDates,
            BarrierTouchStatus.NoTouch,
            _effectiveDate,
            _expirationDate);

        const double expected = 0.0012;
        double actual = _engine.ImpliedCouponRate(template, _ctx, 0);
        Assert.Equal(expected, actual, DefaultTolerance);
    }

    [Fact]
    public void ImpliedCouponRate_DigitalCouponNote_IsAccurate()
    {
        PhoenixOption template = PhoenixOption.CreateDigitalCouponNote(
            0.001,
            1.0,
            0.7,
            1.03,
            _obsDates,
            BarrierTouchStatus.NoTouch,
            _effectiveDate,
            _expirationDate);

        const double expected = 0.0013;
        double actual = _engine.ImpliedCouponRate(template, _ctx, 0);
        Assert.Equal(expected, actual, DefaultTolerance);
    }
}
