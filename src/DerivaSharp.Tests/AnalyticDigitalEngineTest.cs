using DerivaSharp.Instruments;
using DerivaSharp.PricingEngines;

namespace DerivaSharp.Tests;

public class AnalyticDigitalEngineTest
{
    private readonly AnalyticDigitalEngine _engine = new();

    [Theory]
    [InlineData("CashOrNothingCall", 100, 4.612377)]
    [InlineData("CashOrNothingPut", 100, 4.995517)]
    [InlineData("AssetOrNothingCall", 100, 59.274910)]
    [InlineData("AssetOrNothingPut", 100, 39.730074)]
    public void Value_IsAccurate(string optionKind, double assetPrice, double expected)
    {
        const int strike = 100;
        const int rebate = 10;
        DateOnly effectiveDate = new(2025, 1, 6);
        DateOnly expirationDate = effectiveDate.AddDays(365);

        DigitalOption option = optionKind switch
        {
            "CashOrNothingCall" => new CashOrNothingOption(OptionType.Call, strike, rebate, effectiveDate, expirationDate),
            "CashOrNothingPut" => new CashOrNothingOption(OptionType.Put, strike, rebate, effectiveDate, expirationDate),
            "AssetOrNothingCall" => new AssetOrNothingOption(OptionType.Call, strike, effectiveDate, expirationDate),
            "AssetOrNothingPut" => new AssetOrNothingOption(OptionType.Put, strike, effectiveDate, expirationDate),
            _ => throw new ArgumentException("Invalid option kind"),
        };
        PricingContext ctx = new(assetPrice, effectiveDate, 0.3, 0.04, 0.01);

        const int precision = 6;
        Assert.Equal(expected, _engine.Value(option, ctx), precision);
    }

    [Theory]
    [InlineData("CashOrNothingCall", 105, 10)]
    [InlineData("CashOrNothingPut", 95, 10)]
    [InlineData("AssetOrNothingCall", 105, 105)]
    [InlineData("AssetOrNothingPut", 95, 95)]
    public void Value_AtExpiry_ReturnsIntrinsicValue(string optionKind, double assetPrice, double expected)
    {
        const int strike = 100;
        const int rebate = 10;
        DateOnly effectiveDate = new(2025, 1, 6);
        DateOnly expirationDate = effectiveDate.AddDays(365);

        DigitalOption option = optionKind switch
        {
            "CashOrNothingCall" => new CashOrNothingOption(OptionType.Call, strike, rebate, effectiveDate, expirationDate),
            "CashOrNothingPut" => new CashOrNothingOption(OptionType.Put, strike, rebate, effectiveDate, expirationDate),
            "AssetOrNothingCall" => new AssetOrNothingOption(OptionType.Call, strike, effectiveDate, expirationDate),
            "AssetOrNothingPut" => new AssetOrNothingOption(OptionType.Put, strike, effectiveDate, expirationDate),
            _ => throw new ArgumentException("Invalid option kind"),
        };
        PricingContext ctx = new(assetPrice, expirationDate, 0.3, 0.04, 0.01);

        const int precision = 6;
        Assert.Equal(expected, _engine.Value(option, ctx), precision);
    }
}
