using DerivaSharp.Instruments;
using DerivaSharp.Models;
using DerivaSharp.PricingEngines;
using DerivaSharp.Time;

namespace DerivaSharp.Tests;

public class McPhoenixEngineTest
{
    private const double DefaultTolerance = 0.002;
    private readonly DateOnly _effectiveDate;
    private readonly DateOnly _expirationDate;
    private readonly DateOnly[] _obsDates;
    private readonly PricingContext<BsmModel> _ctx;
    private readonly McPhoenixEngine _engine;

    public McPhoenixEngineTest()
    {
        _effectiveDate = new DateOnly(2022, 1, 5);
        _expirationDate = new DateOnly(2023, 1, 5);
        _obsDates = DateUtils.GetObservationDates(_effectiveDate, _expirationDate, 3).ToArray();
        BsmModel model = new(0.16, 0.02, 0.04);
        _ctx = new PricingContext<BsmModel>(model, 1.0, _effectiveDate);
        _engine = new McPhoenixEngine(100000);
    }

    [Fact]
    public void StandardPhoenixValue_IsAccurate()
    {
        BsmModel model = new(0.16, 0.02, 0.04);
        PricingContext<BsmModel> ctx = new(model, 1.0, _effectiveDate);
        PhoenixOption option = PhoenixOption.CreateStandardPhoenix(
            0.0016,
            1.0,
            0.7,
            1.03,
            _obsDates,
            BarrierTouchStatus.NoTouch,
            _effectiveDate,
            _expirationDate);

        McPhoenixEngine engine = new(100000);
        double actual = engine.Value(option, ctx);

        const double expected = 0.0;
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

        const double expected = 0.0016;
        double actual = _engine.ImpliedCouponRate(template, _ctx, 0);
        Assert.Equal(expected, actual, DefaultTolerance);
    }
}
