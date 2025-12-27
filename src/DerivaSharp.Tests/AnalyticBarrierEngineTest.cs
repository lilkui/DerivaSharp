using DerivaSharp.Instruments;
using DerivaSharp.Models;
using DerivaSharp.PricingEngines;

namespace DerivaSharp.Tests;

public class AnalyticBarrierEngineTest
{
    private readonly BsmModel _model = new(0.3, 0.04, 0.01);
    private readonly AnalyticBarrierEngine _engine = new();

    public static TheoryData<OptionType, BarrierType, double, double, PaymentType, int, double, double> ValueTestData => new()
    {
        // OptionType, BarrierType, Barrier, Rebate, PaymentType, ObservationInterval, AssetPrice, Expected
        { OptionType.Call, BarrierType.DownAndIn, 95, 10, PaymentType.PayAtExpiry, 0, 100, 9.383464 },
        { OptionType.Call, BarrierType.DownAndIn, 95, 10, PaymentType.PayAtExpiry, 1, 100, 8.881814 },
        { OptionType.Put, BarrierType.DownAndIn, 95, 10, PaymentType.PayAtExpiry, 0, 100, 11.456302 },
        { OptionType.Put, BarrierType.DownAndIn, 95, 10, PaymentType.PayAtExpiry, 1, 100, 11.672867 },
        { OptionType.Call, BarrierType.UpAndIn, 105, 10, PaymentType.PayAtExpiry, 0, 100, 14.458768 },
        { OptionType.Call, BarrierType.UpAndIn, 105, 10, PaymentType.PayAtExpiry, 1, 100, 14.696871 },
        { OptionType.Put, BarrierType.UpAndIn, 105, 10, PaymentType.PayAtExpiry, 0, 100, 7.979416 },
        { OptionType.Put, BarrierType.UpAndIn, 105, 10, PaymentType.PayAtExpiry, 1, 100, 7.661405 },
        { OptionType.Call, BarrierType.DownAndOut, 95, 10, PaymentType.PayAtHit, 0, 100, 13.673623 },
        { OptionType.Call, BarrierType.DownAndOut, 95, 10, PaymentType.PayAtHit, 1, 100, 14.160558 },
        { OptionType.Put, BarrierType.DownAndOut, 95, 10, PaymentType.PayAtHit, 0, 100, 8.674746 },
        { OptionType.Put, BarrierType.DownAndOut, 95, 10, PaymentType.PayAtHit, 1, 100, 8.443466 },
        { OptionType.Call, BarrierType.UpAndOut, 105, 10, PaymentType.PayAtHit, 0, 100, 8.597574 },
        { OptionType.Put, BarrierType.UpAndOut, 105, 10, PaymentType.PayAtHit, 0, 100, 12.150887 },
        { OptionType.Put, BarrierType.UpAndOut, 105, 10, PaymentType.PayAtHit, 1, 100, 12.453400 },
    };

    public static TheoryData<OptionType, BarrierType, double, double, PaymentType, double, double> ValueAtExpiryTestData => new()
    {
        // OptionType, BarrierType, Barrier, Rebate, PaymentType, AssetPrice, Expected
        { OptionType.Call, BarrierType.DownAndIn, 95, 10, PaymentType.PayAtExpiry, 120, 10 },
        { OptionType.Call, BarrierType.DownAndIn, 95, 10, PaymentType.PayAtExpiry, 80, 0 },
        { OptionType.Call, BarrierType.UpAndIn, 105, 10, PaymentType.PayAtExpiry, 100, 10 },
        { OptionType.Call, BarrierType.UpAndIn, 105, 10, PaymentType.PayAtExpiry, 120, 20 },
        { OptionType.Call, BarrierType.DownAndOut, 95, 10, PaymentType.PayAtHit, 100, 0 },
        { OptionType.Call, BarrierType.DownAndOut, 95, 10, PaymentType.PayAtHit, 80, 10 },
        { OptionType.Call, BarrierType.UpAndOut, 105, 10, PaymentType.PayAtHit, 100, 0 },
        { OptionType.Call, BarrierType.UpAndOut, 105, 10, PaymentType.PayAtHit, 120, 10 },
        { OptionType.Put, BarrierType.DownAndIn, 95, 10, PaymentType.PayAtExpiry, 120, 10 },
        { OptionType.Put, BarrierType.DownAndIn, 95, 10, PaymentType.PayAtExpiry, 80, 20 },
        { OptionType.Put, BarrierType.UpAndIn, 105, 10, PaymentType.PayAtExpiry, 100, 10 },
        { OptionType.Put, BarrierType.UpAndIn, 105, 10, PaymentType.PayAtExpiry, 120, 0 },
        { OptionType.Put, BarrierType.DownAndOut, 95, 10, PaymentType.PayAtHit, 100, 0 },
        { OptionType.Put, BarrierType.DownAndOut, 95, 10, PaymentType.PayAtHit, 80, 10 },
        { OptionType.Put, BarrierType.UpAndOut, 105, 10, PaymentType.PayAtHit, 100, 0 },
        { OptionType.Put, BarrierType.UpAndOut, 105, 10, PaymentType.PayAtHit, 120, 10 },
    };

    [Theory]
    [MemberData(nameof(ValueTestData))]
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
        const double strike = 100;
        DateOnly effectiveDate = new(2025, 1, 6);
        DateOnly expirationDate = effectiveDate.AddDays(365);

        BarrierOption option = new(
            optionType,
            barrierType,
            strike,
            barrier,
            rebate,
            paymentType,
            obsFreq,
            effectiveDate,
            expirationDate);
        PricingContext ctx = new(effectiveDate);
        MarketData market = new(assetPrice);

        const int precision = 6;
        Assert.Equal(expected, _engine.Value(option, _model, market, ctx), precision);
    }

    [Theory]
    [MemberData(nameof(ValueAtExpiryTestData))]
    public void Value_AtExpiry_ReturnsCorrectPayoff(
        OptionType optionType,
        BarrierType barrierType,
        double barrier,
        double rebate,
        PaymentType paymentType,
        double assetPrice,
        double expected)
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
            paymentType,
            0,
            effectiveDate,
            expirationDate);
        PricingContext ctx = new(expirationDate);
        MarketData market = new(assetPrice);

        const int precision = 6;
        Assert.Equal(expected, _engine.Value(option, _model, market, ctx), precision);
    }

    [Fact]
    public void InOutParity_IsSatisfied()
    {
        const double strike = 100;
        const double barrier = 105;
        const double rebate = 10;
        DateOnly effectiveDate = new(2025, 1, 6);
        DateOnly expirationDate = effectiveDate.AddDays(365);

        BarrierOption kiOption = new(
            OptionType.Call,
            BarrierType.UpAndIn,
            strike,
            barrier,
            rebate,
            PaymentType.PayAtExpiry,
            0,
            effectiveDate,
            expirationDate);

        BarrierOption koOption = kiOption with { BarrierType = BarrierType.UpAndOut };
        EuropeanOption eurOption = new(OptionType.Call, strike, effectiveDate, expirationDate);

        const double assetPrice = 100;
        PricingContext ctx = new(effectiveDate);
        MarketData market = new(assetPrice);

        double kiValue = _engine.Value(kiOption, _model, market, ctx);
        double koValue = _engine.Value(koOption, _model, market, ctx);
        double eurValue = new AnalyticEuropeanEngine().Value(eurOption, _model, market, ctx);
        double tau = (expirationDate.DayNumber - effectiveDate.DayNumber) / 365.0;
        double pvRebate = koOption.Rebate * Math.Exp(-_model.RiskFreeRate * tau);

        const int precision = 6;
        Assert.Equal(0, kiValue + koValue - eurValue - pvRebate, precision);
    }
}
