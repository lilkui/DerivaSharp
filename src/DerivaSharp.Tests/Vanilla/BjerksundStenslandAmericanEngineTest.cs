using DerivaSharp.Instruments;
using DerivaSharp.Models;
using DerivaSharp.PricingEngines;
using static DerivaSharp.Tests.AmericanOptionTestData;

namespace DerivaSharp.Tests;

public class BjerksundStenslandAmericanEngineTest
{
    private readonly BjerksundStenslandAmericanEngine _engine = new();

    [Theory]
    [MemberData(nameof(ValueData), MemberType = typeof(AmericanOptionTestData))]
    public void Value_IsAccurate(OptionType optionType, double assetPrice, double expected)
    {
        const int precision = 4;
        AmericanOption option = new(optionType, Strike, EffectiveDate, ExpirationDate);
        PricingContext<BsmModelParameters> ctx = new(ModelParameters, assetPrice, EffectiveDate);
        Assert.Equal(expected, _engine.Value(option, ctx), precision);
    }

    [Theory]
    [MemberData(nameof(ValueAtExpiryData), MemberType = typeof(AmericanOptionTestData))]
    public void Value_AtExpiry_ReturnIntrinsicValue(OptionType optionType, double assetPrice, double expected)
    {
        const int precision = 6;
        AmericanOption option = new(optionType, Strike, EffectiveDate, ExpirationDate);
        PricingContext<BsmModelParameters> ctx = new(ModelParameters, assetPrice, ExpirationDate);
        Assert.Equal(expected, _engine.Value(option, ctx), precision);
    }
}
