using DerivaSharp.Instruments;
using DerivaSharp.Models;
using DerivaSharp.PricingEngines;

namespace DerivaSharp.Tests;

public class FdBarrierEngineTest
{
    private readonly BsmModelParameters _modelParameters = new(0.3, 0.04, 0.01);
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
        PricingContext<BsmModelParameters> ctx = new(_modelParameters, assetPrice, effectiveDate);

        double actual = _fdEngine.Value(option, ctx);
        double expected = _analyticEngine.Value(option, ctx);
        double tolerance = Math.Abs(expected) * 0.0001;
        Assert.Equal(expected, actual, tolerance);
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
        PricingContext<BsmModelParameters> ctx = new(_modelParameters, assetPrice, effectiveDate);

        double kiValue = _fdEngine.Value(kiOption, ctx);
        double koValue = _fdEngine.Value(koOption, ctx);
        double eurValue = new AnalyticEuropeanEngine().Value(eurOption, ctx);
        double tau = (expirationDate.DayNumber - effectiveDate.DayNumber) / 365.0;
        double pvRebate = koOption.Rebate * Math.Exp(-_modelParameters.RiskFreeRate * tau);

        const int precision = 4;
        Assert.Equal(0, kiValue + koValue - eurValue - pvRebate, precision);
    }
}
