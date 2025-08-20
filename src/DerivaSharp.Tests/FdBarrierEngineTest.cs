using DerivaSharp.Instruments;
using DerivaSharp.PricingEngines;

namespace DerivaSharp.Tests;

public class FdBarrierEngineTest
{
    private readonly FdBarrierEngine _fdEngine = new(FiniteDifferenceScheme.CrankNicolson, 1000, 1000);
    private readonly AnalyticBarrierEngine _analyticEngine = new();

    public static TheoryData<OptionType, BarrierType, double, double> ValueTestData => new()
    {
        // OptionType, BarrierType, Barrier, Rebate
        { OptionType.Call, BarrierType.DownAndIn, 95, 10 },
        { OptionType.Put, BarrierType.DownAndIn, 95, 10 },
        { OptionType.Call, BarrierType.UpAndIn, 105, 10 },
        { OptionType.Put, BarrierType.UpAndIn, 105, 10 },
        { OptionType.Call, BarrierType.DownAndOut, 95, 10 },
        { OptionType.Put, BarrierType.DownAndOut, 95, 10 },
        { OptionType.Call, BarrierType.UpAndOut, 105, 10 },
        { OptionType.Put, BarrierType.UpAndOut, 105, 10 },
    };

    [Theory]
    [MemberData(nameof(ValueTestData))]
    public void Value_IsAccurate(OptionType optionType, BarrierType barrierType, double barrier, double rebate)
    {
        const double strike = 100;
        DateOnly effectiveDate = new(2025, 1, 6);
        DateOnly expirationDate = effectiveDate.AddDays(365);

        BarrierOption option = new(
            optionType,
            barrierType,
            strike,
            barrier,
            rebate,
            PaymentType.PayAtExpiry,
            0,
            effectiveDate,
            expirationDate);

        const double assetPrice = 100;
        PricingContext ctx = new(assetPrice, effectiveDate, 0.3, 0.04, 0.01);

        double actual = _fdEngine.Value(option, ctx);
        double expected = _analyticEngine.Value(option, ctx);
        double tolerance = Math.Abs(expected) * 0.0001;
        Assert.Equal(expected, actual, tolerance);
    }
}
