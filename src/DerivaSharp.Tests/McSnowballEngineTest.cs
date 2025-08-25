using DerivaSharp.Instruments;
using DerivaSharp.PricingEngines;

namespace DerivaSharp.Tests;

public class McSnowballEngineTest
{
    private readonly SnowballOption _option;
    private readonly PricingContext _ctx;
    private readonly McSnowballEngine _engine;

    public McSnowballEngineTest()
    {
        DateOnly effectiveDate = new(2022, 1, 5);
        DateOnly expirationDate = new(2023, 1, 5);
        _option = new SnowballOption(
            0.085,
            0.085,
            0.8,
            1.03,
            1.0,
            3,
            BarrierTouchStatus.NoTouch,
            effectiveDate,
            expirationDate);
        _ctx = new PricingContext(1, effectiveDate, 0.16, 0.02, 0.04);
        _engine = new McSnowballEngine(100000);
    }

    [Fact]
    public void Value_IsAccurate()
    {
        const double expected = 0;
        const int precision = 3;
        Assert.Equal(expected, _engine.Value(_option, _ctx), precision);
    }

    [Fact]
    public void Value_PriceAboveKnockOutOnObservationDate_IsAccurate()
    {
        const double expected = 0.021192;
        const int precision = 6;
        PricingContext ctx = _ctx with { AssetPrice = 1.05, ValuationDate = new DateOnly(2022, 4, 6) };
        Assert.Equal(expected, _engine.Value(_option, ctx), precision);
    }
}
