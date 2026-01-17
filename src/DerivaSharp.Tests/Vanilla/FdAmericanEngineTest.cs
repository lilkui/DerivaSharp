using DerivaSharp.Instruments;
using DerivaSharp.Models;
using DerivaSharp.PricingEngines;
using static DerivaSharp.Tests.AmericanOptionTestData;

namespace DerivaSharp.Tests;

public class FdAmericanEngineTest
{
    private readonly FdAmericanEngine _expFdEngine = new(FiniteDifferenceScheme.ExplicitEuler, 200, 4000);
    private readonly FdAmericanEngine _impFdEngine = new(FiniteDifferenceScheme.ImplicitEuler, 1000, 1000);
    private readonly FdAmericanEngine _cnFdEngine = new(FiniteDifferenceScheme.CrankNicolson, 1000, 1000);

    [Theory]
    [MemberData(nameof(FdValueData), MemberType = typeof(AmericanOptionTestData))]
    public void Value_IsAccurate(OptionType optionType, FiniteDifferenceScheme scheme, double toleranceFactor, double expected)
    {
        AmericanOption option = new(optionType, Strike, EffectiveDate, ExpirationDate);
        PricingContext<BsmModelParameters> ctx = new(ModelParameters, AssetPrice, EffectiveDate);

        FdAmericanEngine fdEngine = scheme switch
        {
            FiniteDifferenceScheme.ExplicitEuler => _expFdEngine,
            FiniteDifferenceScheme.ImplicitEuler => _impFdEngine,
            FiniteDifferenceScheme.CrankNicolson => _cnFdEngine,
            _ => throw new ArgumentException("Invalid finite difference scheme"),
        };

        double tolerance = Math.Abs(expected) * toleranceFactor;
        Assert.Equal(expected, fdEngine.Value(option, ctx), tolerance);
    }
}
