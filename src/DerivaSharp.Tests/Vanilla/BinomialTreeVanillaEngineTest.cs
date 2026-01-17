using DerivaSharp.Instruments;
using DerivaSharp.Models;
using DerivaSharp.PricingEngines;
using static DerivaSharp.Tests.EuropeanOptionTestData;

namespace DerivaSharp.Tests;

public class BinomialTreeVanillaEngineTest
{
    private readonly BinomialTreeVanillaEngine _engine = new(1000);

    [Theory]
    [MemberData(nameof(ValueData), MemberType = typeof(EuropeanOptionTestData))]
    public void Value_IsAccurate(OptionType optionType, double assetPrice, double expected)
    {
        EuropeanOption option = new(optionType, Strike, EffectiveDate, ExpirationDate);
        PricingContext<BsmModelParameters> ctx = new(ModelParameters, assetPrice, EffectiveDate);
        double tolerance = Math.Abs(expected) * 0.001;
        Assert.Equal(expected, _engine.Value(option, ctx), tolerance);
    }

    [Theory]
    [MemberData(nameof(ValueAtExpiryData), MemberType = typeof(EuropeanOptionTestData))]
    public void Value_AtExpiry_ReturnsIntrinsicValue(OptionType optionType, double assetPrice, double expected)
    {
        const int precision = 6;
        EuropeanOption option = new(optionType, Strike, EffectiveDate, ExpirationDate);
        PricingContext<BsmModelParameters> ctx = new(ModelParameters, assetPrice, ExpirationDate);
        Assert.Equal(expected, _engine.Value(option, ctx), precision);
    }
}
