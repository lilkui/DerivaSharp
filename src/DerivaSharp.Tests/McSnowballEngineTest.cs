using DerivaSharp.Instruments;
using DerivaSharp.PricingEngines;
using DerivaSharp.Time;

namespace DerivaSharp.Tests;

public class McSnowballEngineTest
{
    private readonly DateOnly _effectiveDate;
    private readonly DateOnly _expirationDate;
    private readonly DateOnly[] _koObsDates;
    private readonly PricingContext _ctx;
    private readonly McSnowballEngine _engine;

    public McSnowballEngineTest()
    {
        _effectiveDate = new DateOnly(2022, 1, 5);
        _expirationDate = new DateOnly(2023, 1, 5);
        _koObsDates = DateUtils.GetObservationDates(_effectiveDate, _expirationDate, 3).ToArray();
        _ctx = new PricingContext(1, _effectiveDate, 0.16, 0.02, 0.04);
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
        const int precision = 3;
        Assert.Equal(expected, _engine.Value(option, _ctx), precision);
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

        PricingContext ctx = _ctx with { AssetPrice = 1.05, ValuationDate = new DateOnly(2022, 4, 6) };

        const double expected = 0.021192;
        const int precision = 6;
        Assert.Equal(expected, _engine.Value(option, ctx), precision);
    }
}
