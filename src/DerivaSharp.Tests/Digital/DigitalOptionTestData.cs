using DerivaSharp.Models;

namespace DerivaSharp.Tests;

public static class DigitalOptionTestData
{
    public const double Strike = 100;
    public const double Rebate = 10;
    public static readonly DateOnly EffectiveDate = new(2025, 1, 6);
    public static readonly DateOnly ExpirationDate = EffectiveDate.AddDays(365);
    public static readonly BsmModelParameters ModelParameters = new(0.3, 0.04, 0.01);

    public static TheoryData<string, double, double> ValueData => new()
    {
        // OptionKind, AssetPrice, ExpectedValue
        { "CashOrNothingCall", 100, 4.612377 },
        { "CashOrNothingPut", 100, 4.995517 },
        { "AssetOrNothingCall", 100, 59.274910 },
        { "AssetOrNothingPut", 100, 39.730074 },
    };

    public static TheoryData<string, double, double> ValueAtExpiryData => new()
    {
        // OptionKind, AssetPrice, ExpectedValue
        { "CashOrNothingCall", 105, 10 },
        { "CashOrNothingPut", 95, 10 },
        { "AssetOrNothingCall", 105, 105 },
        { "AssetOrNothingPut", 95, 95 },
    };
}
