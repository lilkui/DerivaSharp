using DerivaSharp.Instruments;
using DerivaSharp.PricingEngines;

namespace DerivaSharp.Tests;

public class AnalyticBinaryBarrierEngineTest
{
    private readonly AnalyticBinaryBarrierEngine _engine = new();

    public static TheoryData<string, BarrierType, PaymentType, OptionType?, double, double, double, double> ValueTestData => new()
    {
        // OptionKind, BarrierType, PaymentType, OptionType, BarrierPrice, Rebate, AssetPrice, Expected
        { "CashOrNothing", BarrierType.DownAndIn, PaymentType.PayAtHit, null, 90, 10, 100, 7.310536 },
        { "CashOrNothing", BarrierType.UpAndIn, PaymentType.PayAtHit, null, 110, 10, 100, 7.322345 },
        { "AssetOrNothing", BarrierType.DownAndIn, PaymentType.PayAtHit, null, 90, 90, 100, 65.794826 },
        { "AssetOrNothing", BarrierType.UpAndIn, PaymentType.PayAtHit, null, 110, 110, 100, 80.545795 },
        { "CashOrNothing", BarrierType.DownAndIn, PaymentType.PayAtExpiry, null, 90, 10, 100, 7.091270 },
        { "CashOrNothing", BarrierType.UpAndIn, PaymentType.PayAtExpiry, null, 110, 10, 100, 7.097140 },
        { "AssetOrNothing", BarrierType.DownAndIn, PaymentType.PayAtExpiry, null, 90, 0, 100, 65.295307 },
        { "AssetOrNothing", BarrierType.UpAndIn, PaymentType.PayAtExpiry, null, 110, 0, 100, 79.918599 },
        { "CashOrNothing", BarrierType.DownAndOut, PaymentType.PayAtExpiry, null, 90, 10, 100, 2.516625 },
        { "CashOrNothing", BarrierType.UpAndOut, PaymentType.PayAtExpiry, null, 110, 10, 100, 2.510755 },
        { "AssetOrNothing", BarrierType.DownAndOut, PaymentType.PayAtExpiry, null, 90, 0, 100, 33.709677 },
        { "AssetOrNothing", BarrierType.UpAndOut, PaymentType.PayAtExpiry, null, 110, 0, 100, 19.086385 },
        { "CashOrNothing", BarrierType.DownAndIn, PaymentType.PayAtExpiry, OptionType.Call, 90, 10, 100, 2.248046 },
        { "CashOrNothing", BarrierType.UpAndIn, PaymentType.PayAtExpiry, OptionType.Call, 110, 10, 100, 4.499068 },
        { "AssetOrNothing", BarrierType.DownAndIn, PaymentType.PayAtExpiry, OptionType.Call, 90, 0, 100, 27.035296 },
        { "AssetOrNothing", BarrierType.UpAndIn, PaymentType.PayAtExpiry, OptionType.Call, 110, 0, 100, 58.104678 },
        { "CashOrNothing", BarrierType.DownAndIn, PaymentType.PayAtExpiry, OptionType.Put, 90, 10, 100, 4.843224 },
        { "CashOrNothing", BarrierType.UpAndIn, PaymentType.PayAtExpiry, OptionType.Put, 110, 10, 100, 2.598072 },
        { "AssetOrNothing", BarrierType.DownAndIn, PaymentType.PayAtExpiry, OptionType.Put, 90, 0, 100, 38.260011 },
        { "AssetOrNothing", BarrierType.UpAndIn, PaymentType.PayAtExpiry, OptionType.Put, 110, 0, 100, 21.813921 },
        { "CashOrNothing", BarrierType.DownAndOut, PaymentType.PayAtExpiry, OptionType.Call, 90, 10, 100, 2.364332 },
        { "CashOrNothing", BarrierType.UpAndOut, PaymentType.PayAtExpiry, OptionType.Call, 110, 10, 100, 0.113309 },
        { "AssetOrNothing", BarrierType.DownAndOut, PaymentType.PayAtExpiry, OptionType.Call, 90, 0, 100, 32.239613 },
        { "AssetOrNothing", BarrierType.UpAndOut, PaymentType.PayAtExpiry, OptionType.Call, 110, 0, 100, 1.170232 },
        { "CashOrNothing", BarrierType.DownAndOut, PaymentType.PayAtExpiry, OptionType.Put, 90, 10, 100, 0.152293 },
        { "CashOrNothing", BarrierType.UpAndOut, PaymentType.PayAtExpiry, OptionType.Put, 110, 10, 100, 2.397445 },
        { "AssetOrNothing", BarrierType.DownAndOut, PaymentType.PayAtExpiry, OptionType.Put, 90, 0, 100, 1.470063 },
        { "AssetOrNothing", BarrierType.UpAndOut, PaymentType.PayAtExpiry, OptionType.Put, 110, 0, 100, 17.916153 },
    };

    [Theory]
    [MemberData(nameof(ValueTestData))]
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
        const double strike = 100;
        DateOnly effectiveDate = new(2025, 1, 6);
        DateOnly expirationDate = effectiveDate.AddDays(365);

        BinaryBarrierOption option = optionKind switch
        {
            "CashOrNothing" => new CashOrNothingBarrierOption(barrierType, paymentType, optionType, strike, barrierPrice, rebate, 0, effectiveDate, expirationDate),
            "AssetOrNothing" => new AssetOrNothingBarrierOption(barrierType, paymentType, optionType, strike, barrierPrice, rebate, 0, effectiveDate, expirationDate),
            _ => throw new ArgumentException("Invalid option kind"),
        };
        PricingContext ctx = new(assetPrice, effectiveDate, 0.3, 0.04, 0.01);

        const int precision = 6;
        Assert.Equal(expected, _engine.Value(option, ctx), precision);
    }
}
