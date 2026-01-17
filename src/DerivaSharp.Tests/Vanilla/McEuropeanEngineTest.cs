using DerivaSharp.Instruments;
using DerivaSharp.Models;
using DerivaSharp.PricingEngines;
using TorchSharp;
using static DerivaSharp.Tests.EuropeanOptionTestData;

namespace DerivaSharp.Tests;

public class McEuropeanEngineTest
{
    private static readonly bool s_useCuda = torch.cuda.is_available();
    private readonly McEuropeanEngine _engine = new(500000, 2, s_useCuda);

    [Theory]
    [MemberData(nameof(ValueData), MemberType = typeof(EuropeanOptionTestData))]
    public void Value_IsAccurate(OptionType optionType, double assetPrice, double expected)
    {
        EuropeanOption option = new(optionType, Strike, EffectiveDate, ExpirationDate);
        PricingContext<BsmModelParameters> ctx = new(ModelParameters, assetPrice, EffectiveDate);
        double tolerance = Math.Abs(expected) * 0.01;
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
