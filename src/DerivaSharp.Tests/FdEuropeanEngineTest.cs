using DerivaSharp.Instruments;
using DerivaSharp.Models;
using DerivaSharp.PricingEngines;

namespace DerivaSharp.Tests;

public class FdEuropeanEngineTest
{
    private readonly BsmModelParameters _modelParameters = new(0.3, 0.04, 0.01);
    private readonly FdEuropeanEngine _expFdEngine = new(FiniteDifferenceScheme.ExplicitEuler, 200, 4000);
    private readonly FdEuropeanEngine _impFdEngine = new(FiniteDifferenceScheme.ImplicitEuler, 1000, 1000);
    private readonly FdEuropeanEngine _cnFdEngine = new(FiniteDifferenceScheme.CrankNicolson, 1000, 1000);
    private readonly AnalyticEuropeanEngine _analyticEngine = new();

    public static TheoryData<OptionType, FiniteDifferenceScheme, double, double> ValueTestData => new()
    {
        // OptionType, Scheme, AssetPrice, ToleranceFactor
        { OptionType.Call, FiniteDifferenceScheme.ExplicitEuler, 100, 0.001 },
        { OptionType.Put, FiniteDifferenceScheme.ExplicitEuler, 100, 0.001 },
        { OptionType.Call, FiniteDifferenceScheme.ImplicitEuler, 100, 0.0005 },
        { OptionType.Put, FiniteDifferenceScheme.ImplicitEuler, 100, 0.0005 },
        { OptionType.Call, FiniteDifferenceScheme.CrankNicolson, 100, 0.0005 },
        { OptionType.Put, FiniteDifferenceScheme.CrankNicolson, 100, 0.0005 },
    };

    [Theory]
    [MemberData(nameof(ValueTestData))]
    public void Value_IsAccurate(OptionType optionType, FiniteDifferenceScheme scheme, double assetPrice, double toleranceFactor)
    {
        const double strike = 100;
        DateOnly effectiveDate = new(2025, 1, 6);
        DateOnly expirationDate = effectiveDate.AddDays(365);

        EuropeanOption option = new(optionType, strike, effectiveDate, expirationDate);
        PricingContext<BsmModelParameters> ctx = new(_modelParameters, assetPrice, effectiveDate);

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
