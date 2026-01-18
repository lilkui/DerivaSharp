using DerivaSharp.Instruments;
using DerivaSharp.Models;
using DerivaSharp.PricingEngines;
using static DerivaSharp.Tests.DigitalOptionTestData;

namespace DerivaSharp.Tests;

public class FdDigitalEngineTest
{
    private readonly FdDigitalEngine _fdEngine = new(FiniteDifferenceScheme.CrankNicolson, 1000, 1000);

    [Theory]
    [MemberData(nameof(ValueData), MemberType = typeof(DigitalOptionTestData))]
    public void Value_IsAccurate(string optionKind, double assetPrice, double expected)
    {
        DigitalOption option = optionKind switch
        {
            "CashOrNothingCall" => new CashOrNothingOption(OptionType.Call, Strike, Rebate, EffectiveDate, ExpirationDate),
            "CashOrNothingPut" => new CashOrNothingOption(OptionType.Put, Strike, Rebate, EffectiveDate, ExpirationDate),
            "AssetOrNothingCall" => new AssetOrNothingOption(OptionType.Call, Strike, EffectiveDate, ExpirationDate),
            "AssetOrNothingPut" => new AssetOrNothingOption(OptionType.Put, Strike, EffectiveDate, ExpirationDate),
            _ => throw new ArgumentException("Invalid option kind"),
        };
        PricingContext<BsmModelParameters> ctx = new(ModelParameters, assetPrice, EffectiveDate);
        double actual = _fdEngine.Value(option, ctx);
        double tolerance = Math.Abs(expected) * 0.001;
        Assert.Equal(expected, actual, tolerance);
    }
}
