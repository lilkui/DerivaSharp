using DerivaSharp.Instruments;
using DerivaSharp.Models;
using DerivaSharp.PricingEngines;
using static DerivaSharp.Tests.EuropeanOptionTestData;

namespace DerivaSharp.Tests;

public class FdEuropeanEngineTest
{
    private readonly FdEuropeanEngine _expFdEngine = new(FiniteDifferenceScheme.ExplicitEuler, 200, 4000);
    private readonly FdEuropeanEngine _impFdEngine = new(FiniteDifferenceScheme.ImplicitEuler, 1000, 1000);
    private readonly FdEuropeanEngine _cnFdEngine = new(FiniteDifferenceScheme.CrankNicolson, 1000, 1000);
    private readonly AnalyticEuropeanEngine _analyticEngine = new();

    [Theory]
    [MemberData(nameof(FdParameters), MemberType = typeof(EuropeanOptionTestData))]
    public void Value_IsAccurate(OptionType optionType, FiniteDifferenceScheme scheme, double toleranceFactor)
    {
        const double assetPrice = 100;
        EuropeanOption option = new(optionType, Strike, EffectiveDate, ExpirationDate);
        PricingContext<BsmModelParameters> ctx = new(ModelParameters, assetPrice, EffectiveDate);

        FdEuropeanEngine fdEngine = scheme switch
        {
            FiniteDifferenceScheme.ExplicitEuler => _expFdEngine,
            FiniteDifferenceScheme.ImplicitEuler => _impFdEngine,
            FiniteDifferenceScheme.CrankNicolson => _cnFdEngine,
            _ => throw new ArgumentException("Invalid finite difference scheme"),
        };

        double expected = _analyticEngine.Value(option, ctx);
        double value = fdEngine.Value(option, ctx);
        double tolerance = Math.Abs(expected) * toleranceFactor;
        Assert.Equal(expected, value, tolerance);
    }
}
