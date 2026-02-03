using DerivaSharp.Instruments;
using DerivaSharp.Models;
using DerivaSharp.PricingEngines;
using static DerivaSharp.Tests.DigitalOptionTestData;

namespace DerivaSharp.Tests;

public class AnalyticDigitalEngineTest
{
    private readonly AnalyticDigitalEngine _engine = new();

    [Theory]
    [MemberData(nameof(ValueData), MemberType = typeof(DigitalOptionTestData))]
    public void Value_IsAccurate(string optionKind, double assetPrice, double expected)
    {
        const int precision = 6;
        DigitalOption option = optionKind switch
        {
            "CashOrNothingCall" => new CashOrNothingOption(OptionType.Call, Strike, Rebate, EffectiveDate, ExpirationDate),
            "CashOrNothingPut" => new CashOrNothingOption(OptionType.Put, Strike, Rebate, EffectiveDate, ExpirationDate),
            "AssetOrNothingCall" => new AssetOrNothingOption(OptionType.Call, Strike, EffectiveDate, ExpirationDate),
            "AssetOrNothingPut" => new AssetOrNothingOption(OptionType.Put, Strike, EffectiveDate, ExpirationDate),
            _ => throw new ArgumentException("Invalid option kind"),
        };
        PricingContext<BsmModelParameters> ctx = new(ModelParameters, assetPrice, EffectiveDate);
        Assert.Equal(expected, _engine.Value(option, ctx), precision);
    }

    [Theory]
    [MemberData(nameof(ValueAtExpiryData), MemberType = typeof(DigitalOptionTestData))]
    public void Value_AtExpiry_ReturnsIntrinsicValue(string optionKind, double assetPrice, double expected)
    {
        const int precision = 6;
        DigitalOption option = optionKind switch
        {
            "CashOrNothingCall" => new CashOrNothingOption(OptionType.Call, Strike, Rebate, EffectiveDate, ExpirationDate),
            "CashOrNothingPut" => new CashOrNothingOption(OptionType.Put, Strike, Rebate, EffectiveDate, ExpirationDate),
            "AssetOrNothingCall" => new AssetOrNothingOption(OptionType.Call, Strike, EffectiveDate, ExpirationDate),
            "AssetOrNothingPut" => new AssetOrNothingOption(OptionType.Put, Strike, EffectiveDate, ExpirationDate),
            _ => throw new ArgumentException("Invalid option kind"),
        };
        PricingContext<BsmModelParameters> ctx = new(ModelParameters, assetPrice, ExpirationDate);
        Assert.Equal(expected, _engine.Value(option, ctx), precision);
    }
}
