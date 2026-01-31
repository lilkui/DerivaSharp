using DerivaSharp.Instruments;
using DerivaSharp.Models;
using DerivaSharp.PricingEngines;
using static DerivaSharp.Tests.Asian.AsianOptionTestData;

namespace DerivaSharp.Tests.Asian;

public class ArithmeticAverageAsianEngineTest
{
    private readonly ArithmeticAverageAsianEngine _engine = new();

    [Theory]
    [MemberData(nameof(ArithmeticValueData), MemberType = typeof(AsianOptionTestData))]
    public void Value_IsAccurate(OptionType optionType, double assetPrice, double realizedAveragePrice, double expected)
    {
        const int precision = 6;
        ArithmeticAverageOption option = new(
            optionType,
            Strike,
            EffectiveDate,
            realizedAveragePrice,
            EffectiveDate,
            ExpirationDate);
        PricingContext<BsmModelParameters> ctx = new(ModelParameters, assetPrice, ValuationDate);
        double actual = _engine.Value(option, ctx);
        Assert.Equal(expected, actual, precision);
    }
}
