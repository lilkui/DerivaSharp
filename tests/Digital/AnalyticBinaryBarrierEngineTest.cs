using DerivaSharp.Instruments;
using DerivaSharp.Models;
using DerivaSharp.PricingEngines;
using static DerivaSharp.Tests.BinaryBarrierOptionTestData;

namespace DerivaSharp.Tests;

public class AnalyticBinaryBarrierEngineTest
{
    private readonly AnalyticBinaryBarrierEngine _engine = new();

    [Theory]
    [MemberData(nameof(ValueData), MemberType = typeof(BinaryBarrierOptionTestData))]
    public void Value_IsAccurate(
        string optionKind,
        BarrierType barrierType,
        PaymentType paymentType,
        OptionType? optionType,
        double barrierPrice,
        double rebate,
        double assetPrice,
        double expected)
    {
        BinaryBarrierOption option = optionKind switch
        {
            "CashOrNothing" => new CashOrNothingBarrierOption(barrierType, paymentType, optionType, Strike, barrierPrice, rebate, 0, EffectiveDate, ExpirationDate),
            "AssetOrNothing" => new AssetOrNothingBarrierOption(barrierType, paymentType, optionType, Strike, barrierPrice, rebate, 0, EffectiveDate, ExpirationDate),
            _ => throw new ArgumentException("Invalid option kind"),
        };
        PricingContext<BsmModelParameters> ctx = new(ModelParameters, assetPrice, EffectiveDate);

        const int precision = 6;
        Assert.Equal(expected, _engine.Value(option, ctx), precision);
    }
}
