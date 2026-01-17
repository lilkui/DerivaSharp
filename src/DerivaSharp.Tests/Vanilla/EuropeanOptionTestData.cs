using DerivaSharp.Instruments;
using DerivaSharp.Models;
using DerivaSharp.PricingEngines;

namespace DerivaSharp.Tests;

public static class EuropeanOptionTestData
{
    public const double Strike = 100;
    public static readonly DateOnly EffectiveDate = new(2025, 1, 6);
    public static readonly DateOnly ExpirationDate = EffectiveDate.AddDays(365);
    public static readonly BsmModelParameters ModelParameters = new(0.3, 0.04, 0.01);

    public static TheoryData<OptionType, double, double> ValueData => new()
    {
        // OptionType, AssetPrice, ExpectedValue
        { OptionType.Call, 100, 13.151137 },
        { OptionType.Put, 100, 10.225098 },
    };

    public static TheoryData<OptionType, double, double> ValueAtExpiryData => new()
    {
        // OptionType, AssetPrice, ExpectedValue
        { OptionType.Call, 110, 10 },
        { OptionType.Put, 90, 10 },
    };

    public static TheoryData<string, OptionType, double, double> GreekData => new()
    {
        // Greek, OptionType, AssetPrice, ExpectedValue
        { "Delta", OptionType.Call, 100, 0.592749 },
        { "Delta", OptionType.Put, 100, -0.397301 },
        { "Gamma", OptionType.Call, 100, 0.012761 },
        { "Gamma", OptionType.Put, 100, 0.012761 },
        { "Speed", OptionType.Call, 100, -0.000234 },
        { "Speed", OptionType.Put, 100, -0.000234 },
        { "Theta", OptionType.Call, 100, -0.019163 },
        { "Theta", OptionType.Put, 100, -0.011346 },
        { "Vega", OptionType.Call, 100, 0.382821 },
        { "Vega", OptionType.Put, 100, 0.382821 },
        { "Vanna", OptionType.Call, 100, 0.000638 },
        { "Vanna", OptionType.Put, 100, 0.000638 },
        { "Zomma", OptionType.Call, 100, -0.000431 },
        { "Zomma", OptionType.Put, 100, -0.000431 },
        { "Rho", OptionType.Call, 100, 0.461238 },
        { "Rho", OptionType.Put, 100, -0.499552 },
    };

    public static TheoryData<double, double, double> ImpliedVolatilityData => new()
    {
        // Price, AssetPrice, ExpectedValue
        { 13.1511, 100, 0.3 },
        { 5, 100, 0.0857 },
        { 90, 100, 3.3664 },
    };

    public static TheoryData<OptionType, FiniteDifferenceScheme, double> FdParameters => new()
    {
        // OptionType, Scheme, ToleranceFactor
        { OptionType.Call, FiniteDifferenceScheme.ExplicitEuler, 0.001 },
        { OptionType.Put, FiniteDifferenceScheme.ExplicitEuler, 0.001 },
        { OptionType.Call, FiniteDifferenceScheme.ImplicitEuler, 0.0005 },
        { OptionType.Put, FiniteDifferenceScheme.ImplicitEuler, 0.0005 },
        { OptionType.Call, FiniteDifferenceScheme.CrankNicolson, 0.0005 },
        { OptionType.Put, FiniteDifferenceScheme.CrankNicolson, 0.0005 },
    };
}
