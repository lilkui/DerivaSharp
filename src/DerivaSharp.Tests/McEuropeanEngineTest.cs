using DerivaSharp.Instruments;
using DerivaSharp.Models;
using DerivaSharp.PricingEngines;

namespace DerivaSharp.Tests;

public class McEuropeanEngineTest
{
    private readonly BsmModel _model = new(0.3, 0.04, 0.01);
    private readonly McEuropeanEngine _engine = new(500000, 2);

    [Theory]
    [InlineData(OptionType.Call, 100, 13.1511)]
    [InlineData(OptionType.Put, 100, 10.2251)]
    public void Value_IsAccurate(OptionType optionType, double assetPrice, double expected)
    {
        const double strike = 100;
        DateOnly effectiveDate = new(2025, 1, 6);
        DateOnly expirationDate = effectiveDate.AddDays(365);

        EuropeanOption option = new(optionType, strike, effectiveDate, expirationDate);
        PricingContext<BsmModel> ctx = new(_model, assetPrice, effectiveDate);

        double tolerance = Math.Abs(expected) * 0.01;
        Assert.Equal(expected, _engine.Value(option, ctx), tolerance);
    }

    [Theory]
    [InlineData(OptionType.Call, 110, 10)]
    [InlineData(OptionType.Put, 90, 10)]
    public void Value_AtExpiry_ReturnsIntrinsicValue(OptionType optionType, double assetPrice, double expected)
    {
        const double strike = 100;
        DateOnly effectiveDate = new(2025, 1, 6);
        DateOnly expirationDate = effectiveDate.AddDays(365);

        EuropeanOption option = new(optionType, strike, effectiveDate, expirationDate);
        PricingContext<BsmModel> ctx = new(_model, assetPrice, expirationDate);

        const int precision = 6;
        Assert.Equal(expected, _engine.Value(option, ctx), precision);
    }
}
