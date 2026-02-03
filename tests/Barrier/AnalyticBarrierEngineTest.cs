using DerivaSharp.Instruments;
using DerivaSharp.Models;
using DerivaSharp.PricingEngines;
using static DerivaSharp.Tests.BarrierOptionTestData;

namespace DerivaSharp.Tests;

public class AnalyticBarrierEngineTest
{
    private readonly AnalyticBarrierEngine _engine = new();

    [Theory]
    [MemberData(nameof(ValueData), MemberType = typeof(BarrierOptionTestData))]
    public void Value_IsAccurate(
        OptionType optionType,
        BarrierType barrierType,
        double barrier,
        double rebate,
        PaymentType paymentType,
        int obsFreq,
        double assetPrice,
        double expected)
    {
        const int precision = 6;
        BarrierOption option = new(
            optionType,
            barrierType,
            Strike,
            barrier,
            rebate,
            paymentType,
            obsFreq,
            EffectiveDate,
            ExpirationDate);
        PricingContext<BsmModelParameters> ctx = new(ModelParameters, assetPrice, EffectiveDate);
        Assert.Equal(expected, _engine.Value(option, ctx), precision);
    }

    [Theory]
    [MemberData(nameof(ValueAtExpiryData), MemberType = typeof(BarrierOptionTestData))]
    public void Value_AtExpiry_ReturnsCorrectPayoff(
        OptionType optionType,
        BarrierType barrierType,
        double barrier,
        double rebate,
        PaymentType paymentType,
        double assetPrice,
        double expected)
    {
        const int precision = 6;
        BarrierOption option = new(
            optionType,
            barrierType,
            Strike,
            barrier,
            rebate,
            paymentType,
            0,
            EffectiveDate,
            ExpirationDate);
        PricingContext<BsmModelParameters> ctx = new(ModelParameters, assetPrice, ExpirationDate);
        Assert.Equal(expected, _engine.Value(option, ctx), precision);
    }

    [Fact]
    public void InOutParity_IsSatisfied()
    {
        const int precision = 6;
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
        double kiValue = _engine.Value(kiOption, ctx);
        double koValue = _engine.Value(koOption, ctx);
        double eurValue = new AnalyticEuropeanEngine().Value(eurOption, ctx);
        double tau = (ExpirationDate.DayNumber - EffectiveDate.DayNumber) / 365.0;
        double pvRebate = koOption.Rebate * Math.Exp(-ModelParameters.RiskFreeRate * tau);
        Assert.Equal(0, kiValue + koValue - eurValue - pvRebate, precision);
    }
}
