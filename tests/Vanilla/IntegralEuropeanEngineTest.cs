using DerivaSharp.Instruments;
using DerivaSharp.Models;
using DerivaSharp.PricingEngines;
using static DerivaSharp.Tests.EuropeanOptionTestData;

namespace DerivaSharp.Tests;

public class IntegralEuropeanEngineTest
{
    private readonly IntegralEuropeanEngine _engine = new();

    [Theory]
    [MemberData(nameof(ValueData), MemberType = typeof(EuropeanOptionTestData))]
    public void Value_IsAccurate(OptionType optionType, double assetPrice, double expected)
    {
        EuropeanOption option = new(optionType, Strike, EffectiveDate, ExpirationDate);
        PricingContext<BsmModelParameters> ctx = new(ModelParameters, assetPrice, EffectiveDate);
        double tolerance = Math.Abs(expected) * 0.0001;
        double actual = _engine.Value(option, ctx);
        Assert.Equal(expected, actual, tolerance);
    }

    [Theory]
    [MemberData(nameof(ValueAtExpiryData), MemberType = typeof(EuropeanOptionTestData))]
    public void Value_AtExpiry_ReturnsIntrinsicValue(OptionType optionType, double assetPrice, double expected)
    {
        const int precision = 6;
        EuropeanOption option = new(optionType, Strike, EffectiveDate, ExpirationDate);
        PricingContext<BsmModelParameters> ctx = new(ModelParameters, assetPrice, ExpirationDate);
        double actual = _engine.Value(option, ctx);
        Assert.Equal(expected, actual, precision);
    }
}
