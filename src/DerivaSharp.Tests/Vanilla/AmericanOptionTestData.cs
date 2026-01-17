using DerivaSharp.Instruments;
using DerivaSharp.Models;
using DerivaSharp.PricingEngines;

namespace DerivaSharp.Tests;

public static class AmericanOptionTestData
{
    public const double Strike = 100;
    public const double AssetPrice = 100;
    public static readonly DateOnly EffectiveDate = new(2025, 1, 6);
    public static readonly DateOnly ExpirationDate = EffectiveDate.AddDays(365);
    public static readonly BsmModelParameters ModelParameters = new(0.3, 0.04, 0.02);

    public static TheoryData<OptionType, double, double> ValueData => new()
    {
        // OptionType, AssetPrice, ExpectedValue
        { OptionType.Call, 100, 12.5682 },
        { OptionType.Put, 100, 10.8264 },
    };

    public static TheoryData<OptionType, double, double> ValueAtExpiryData => new()
    {
        // OptionType, AssetPrice, ExpectedValue
        { OptionType.Call, 110, 10 },
        { OptionType.Put, 90, 10 },
    };

    public static TheoryData<OptionType, FiniteDifferenceScheme, double, double> FdValueData => new()
    {
        // OptionType, Scheme, ToleranceFactor, ExpectedValue
        { OptionType.Call, FiniteDifferenceScheme.ExplicitEuler, 0.001, 12.5682 },
        { OptionType.Put, FiniteDifferenceScheme.ExplicitEuler, 0.005, 10.8264 },
        { OptionType.Call, FiniteDifferenceScheme.ImplicitEuler, 0.001, 12.5682 },
        { OptionType.Put, FiniteDifferenceScheme.ImplicitEuler, 0.005, 10.8264 },
        { OptionType.Call, FiniteDifferenceScheme.CrankNicolson, 0.001, 12.5682 },
        { OptionType.Put, FiniteDifferenceScheme.CrankNicolson, 0.005, 10.8264 },
    };
}
