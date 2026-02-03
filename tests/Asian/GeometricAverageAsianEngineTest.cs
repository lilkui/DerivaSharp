using DerivaSharp.Instruments;
using DerivaSharp.Models;
using DerivaSharp.PricingEngines;

namespace DerivaSharp.Tests.Asian;

public class GeometricAverageAsianEngineTest
{
    [Fact]
    public void Value_IsAccurate()
    {
        const double strike = 85;
        DateOnly effectiveDate = new(2025, 1, 6);
        DateOnly expirationDate = effectiveDate.AddDays(91);
        GeometricAverageOption option = new(OptionType.Put, strike, effectiveDate, expirationDate);
        BsmModelParameters modelParameters = new(0.2, 0.05, -0.03);
        PricingContext<BsmModelParameters> ctx = new(modelParameters, 80, effectiveDate);
        GeometricAverageAsianEngine engine = new();
        double actual = engine.Value(option, ctx);
        const double expected = 4.6923;
        const int precision = 4;
        Assert.Equal(expected, actual, precision);
    }
}
