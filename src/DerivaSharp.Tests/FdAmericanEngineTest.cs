using DerivaSharp.Instruments;
using DerivaSharp.Models;
using DerivaSharp.PricingEngines;

namespace DerivaSharp.Tests;

public class FdAmericanEngineTest
{
    private readonly BsmModel _model = new(0.3, 0.04, 0.02);
    private readonly FdAmericanEngine _expFdEngine = new(FiniteDifferenceScheme.ExplicitEuler, 200, 4000);
    private readonly FdAmericanEngine _impFdEngine = new(FiniteDifferenceScheme.ImplicitEuler, 1000, 1000);
    private readonly FdAmericanEngine _cnFdEngine = new(FiniteDifferenceScheme.CrankNicolson, 1000, 1000);

    public static TheoryData<OptionType, FiniteDifferenceScheme, double, double, double> ValueTestData => new()
    {
        // OptionType, Scheme, AssetPrice, ToleranceFactor, Expected
        { OptionType.Call, FiniteDifferenceScheme.ExplicitEuler, 100, 0.001, 12.5682 },
        { OptionType.Put, FiniteDifferenceScheme.ExplicitEuler, 100, 0.005, 10.8264 },
        { OptionType.Call, FiniteDifferenceScheme.ImplicitEuler, 100, 0.001, 12.5682 },
        { OptionType.Put, FiniteDifferenceScheme.ImplicitEuler, 100, 0.005, 10.8264 },
        { OptionType.Call, FiniteDifferenceScheme.CrankNicolson, 100, 0.001, 12.5682 },
        { OptionType.Put, FiniteDifferenceScheme.CrankNicolson, 100, 0.005, 10.8264 },
    };

    [Theory]
    [MemberData(nameof(ValueTestData))]
    public void Value_IsAccurate(OptionType optionType, FiniteDifferenceScheme scheme, double assetPrice, double toleranceFactor, double expected)
    {
        const double strike = 100;
        DateOnly effectiveDate = new(2025, 1, 6);
        DateOnly expirationDate = effectiveDate.AddDays(365);

        AmericanOption option = new(optionType, strike, effectiveDate, expirationDate);
        PricingContext<BsmModel> ctx = new(_model, assetPrice, effectiveDate);

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
