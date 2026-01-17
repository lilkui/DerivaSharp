using DerivaSharp.Instruments;
using DerivaSharp.Models;
using DerivaSharp.PricingEngines;
using static DerivaSharp.Tests.BarrierOptionTestData;

namespace DerivaSharp.Tests;

public class FdBarrierEngineTest
{
    private readonly FdBarrierEngine _fdEngine = new(FiniteDifferenceScheme.CrankNicolson, 1000, 1000);
    private readonly AnalyticBarrierEngine _analyticEngine = new();

    [Theory]
    [MemberData(nameof(FdParameters), MemberType = typeof(BarrierOptionTestData))]
    public void Value_IsAccurate(OptionType optionType, BarrierType barrierType, double barrier, double rebate, double assetPrice)
    {
        BarrierOption option = new(
            optionType,
            barrierType,
            Strike,
            barrier,
            rebate,
            PaymentType.PayAtExpiry,
            0,
            EffectiveDate,
            ExpirationDate);
        PricingContext<BsmModelParameters> ctx = new(ModelParameters, assetPrice, EffectiveDate);
        double actual = _fdEngine.Value(option, ctx);
        double expected = _analyticEngine.Value(option, ctx);
        double tolerance = Math.Abs(expected) * 0.0001;
        Assert.Equal(expected, actual, tolerance);
    }

    [Fact]
    public void InOutParity_IsSatisfied()
    {
        const int precision = 4;
        const double assetPrice = 100;
        BarrierOption kiOption = new(
            OptionType.Call,
            BarrierType.UpAndIn,
            Strike,
            105,
            10,
            PaymentType.PayAtExpiry,
            0,
            EffectiveDate,
            ExpirationDate);
        BarrierOption koOption = kiOption with { BarrierType = BarrierType.UpAndOut };
        EuropeanOption eurOption = new(OptionType.Call, Strike, EffectiveDate, ExpirationDate);
        PricingContext<BsmModelParameters> ctx = new(ModelParameters, assetPrice, EffectiveDate);
        double kiValue = _fdEngine.Value(kiOption, ctx);
        double koValue = _fdEngine.Value(koOption, ctx);
        double eurValue = new AnalyticEuropeanEngine().Value(eurOption, ctx);
        double tau = (ExpirationDate.DayNumber - EffectiveDate.DayNumber) / 365.0;
        double pvRebate = koOption.Rebate * Math.Exp(-ModelParameters.RiskFreeRate * tau);
        Assert.Equal(0, kiValue + koValue - eurValue - pvRebate, precision);
    }
}
