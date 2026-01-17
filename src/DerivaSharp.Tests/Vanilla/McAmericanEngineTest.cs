using DerivaSharp.Instruments;
using DerivaSharp.Models;
using DerivaSharp.PricingEngines;
using TorchSharp;
using static DerivaSharp.Tests.AmericanOptionTestData;

namespace DerivaSharp.Tests;

public class McAmericanEngineTest
{
    private static readonly bool s_useCuda = torch.cuda.is_available();
    private readonly McAmericanEngine _engine = new(200000, 250, s_useCuda);

    [Theory]
    [MemberData(nameof(ValueData), MemberType = typeof(AmericanOptionTestData))]
    public void Value_IsAccurate(OptionType optionType, double assetPrice, double expected)
    {
        AmericanOption option = new(optionType, Strike, EffectiveDate, ExpirationDate);
        PricingContext<BsmModelParameters> ctx = new(ModelParameters, assetPrice, EffectiveDate);
        double tolerance = Math.Abs(expected) * 0.02;
        Assert.Equal(expected, _engine.Value(option, ctx), tolerance);
    }

    [Theory]
    [MemberData(nameof(ValueAtExpiryData), MemberType = typeof(AmericanOptionTestData))]
    public void Value_AtExpiry_ReturnsIntrinsicValue(OptionType optionType, double assetPrice, double expected)
    {
        const int precision = 6;
        AmericanOption option = new(optionType, Strike, EffectiveDate, ExpirationDate);
        PricingContext<BsmModelParameters> ctx = new(ModelParameters, assetPrice, ExpirationDate);
        Assert.Equal(expected, _engine.Value(option, ctx), precision);
    }
}
