using DerivaSharp.Instruments;
using DerivaSharp.Models;

namespace DerivaSharp.Tests;

public static class BarrierOptionTestData
{
    public const double Strike = 100;
    public static readonly DateOnly EffectiveDate = new(2025, 1, 6);
    public static readonly DateOnly ExpirationDate = EffectiveDate.AddDays(365);
    public static readonly BsmModelParameters ModelParameters = new(0.3, 0.04, 0.01);

    public static TheoryData<OptionType, BarrierType, double, double, PaymentType, int, double, double> ValueData => new()
    {
        // OptionType, BarrierType, Barrier, Rebate, PaymentType, ObservationInterval, AssetPrice, Expected
        { OptionType.Call, BarrierType.DownAndIn, 95, 10, PaymentType.PayAtExpiry, 0, 100, 9.383464 },
        { OptionType.Call, BarrierType.DownAndIn, 95, 10, PaymentType.PayAtExpiry, 1, 100, 8.881814 },
        { OptionType.Put, BarrierType.DownAndIn, 95, 10, PaymentType.PayAtExpiry, 0, 100, 11.456302 },
        { OptionType.Put, BarrierType.DownAndIn, 95, 10, PaymentType.PayAtExpiry, 1, 100, 11.672867 },
        { OptionType.Call, BarrierType.UpAndIn, 105, 10, PaymentType.PayAtExpiry, 0, 100, 14.458768 },
        { OptionType.Call, BarrierType.UpAndIn, 105, 10, PaymentType.PayAtExpiry, 1, 100, 14.696871 },
        { OptionType.Put, BarrierType.UpAndIn, 105, 10, PaymentType.PayAtExpiry, 0, 100, 7.979416 },
        { OptionType.Put, BarrierType.UpAndIn, 105, 10, PaymentType.PayAtExpiry, 1, 100, 7.661405 },
        { OptionType.Call, BarrierType.DownAndOut, 95, 10, PaymentType.PayAtHit, 0, 100, 13.673623 },
        { OptionType.Call, BarrierType.DownAndOut, 95, 10, PaymentType.PayAtHit, 1, 100, 14.160558 },
        { OptionType.Put, BarrierType.DownAndOut, 95, 10, PaymentType.PayAtHit, 0, 100, 8.674746 },
        { OptionType.Put, BarrierType.DownAndOut, 95, 10, PaymentType.PayAtHit, 1, 100, 8.443466 },
        { OptionType.Call, BarrierType.UpAndOut, 105, 10, PaymentType.PayAtHit, 0, 100, 8.597574 },
        { OptionType.Put, BarrierType.UpAndOut, 105, 10, PaymentType.PayAtHit, 0, 100, 12.150887 },
        { OptionType.Put, BarrierType.UpAndOut, 105, 10, PaymentType.PayAtHit, 1, 100, 12.453400 },
    };

    public static TheoryData<OptionType, BarrierType, double, double, PaymentType, double, double> ValueAtExpiryData => new()
    {
        // OptionType, BarrierType, Barrier, Rebate, PaymentType, AssetPrice, Expected
        { OptionType.Call, BarrierType.DownAndIn, 95, 10, PaymentType.PayAtExpiry, 120, 10 },
        { OptionType.Call, BarrierType.DownAndIn, 95, 10, PaymentType.PayAtExpiry, 80, 0 },
        { OptionType.Call, BarrierType.UpAndIn, 105, 10, PaymentType.PayAtExpiry, 100, 10 },
        { OptionType.Call, BarrierType.UpAndIn, 105, 10, PaymentType.PayAtExpiry, 120, 20 },
        { OptionType.Call, BarrierType.DownAndOut, 95, 10, PaymentType.PayAtHit, 100, 0 },
        { OptionType.Call, BarrierType.DownAndOut, 95, 10, PaymentType.PayAtHit, 80, 10 },
        { OptionType.Call, BarrierType.UpAndOut, 105, 10, PaymentType.PayAtHit, 100, 0 },
        { OptionType.Call, BarrierType.UpAndOut, 105, 10, PaymentType.PayAtHit, 120, 10 },
        { OptionType.Put, BarrierType.DownAndIn, 95, 10, PaymentType.PayAtExpiry, 120, 10 },
        { OptionType.Put, BarrierType.DownAndIn, 95, 10, PaymentType.PayAtExpiry, 80, 20 },
        { OptionType.Put, BarrierType.UpAndIn, 105, 10, PaymentType.PayAtExpiry, 100, 10 },
        { OptionType.Put, BarrierType.UpAndIn, 105, 10, PaymentType.PayAtExpiry, 120, 0 },
        { OptionType.Put, BarrierType.DownAndOut, 95, 10, PaymentType.PayAtHit, 100, 0 },
        { OptionType.Put, BarrierType.DownAndOut, 95, 10, PaymentType.PayAtHit, 80, 10 },
        { OptionType.Put, BarrierType.UpAndOut, 105, 10, PaymentType.PayAtHit, 100, 0 },
        { OptionType.Put, BarrierType.UpAndOut, 105, 10, PaymentType.PayAtHit, 120, 10 },
    };

    public static TheoryData<OptionType, BarrierType, double, double, double> FdParameters => new()
    {
        // OptionType, BarrierType, Barrier, Rebate, AssetPrice
        { OptionType.Call, BarrierType.DownAndIn, 95, 10, 100 },
        { OptionType.Put, BarrierType.DownAndIn, 95, 10, 100 },
        { OptionType.Call, BarrierType.UpAndIn, 105, 10, 100 },
        { OptionType.Put, BarrierType.UpAndIn, 105, 10, 100 },
        { OptionType.Call, BarrierType.DownAndOut, 95, 10, 100 },
        { OptionType.Put, BarrierType.DownAndOut, 95, 10, 100 },
        { OptionType.Call, BarrierType.UpAndOut, 105, 10, 100 },
        { OptionType.Put, BarrierType.UpAndOut, 105, 10, 100 },
    };
}
