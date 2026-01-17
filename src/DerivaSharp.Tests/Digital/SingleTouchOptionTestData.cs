using DerivaSharp.Instruments;
using DerivaSharp.Models;

namespace DerivaSharp.Tests;

public static class SingleTouchOptionTestData
{
    public const double Strike = 100;
    public static readonly DateOnly EffectiveDate = new(2025, 1, 6);
    public static readonly DateOnly ExpirationDate = EffectiveDate.AddDays(365);
    public static readonly BsmModelParameters ModelParameters = new(0.3, 0.04, 0.01);

    public static TheoryData<TouchType, PaymentType, double, double, double> ValueData => new()
    {
        // TouchType, PaymentType, Barrier, Rebate, AssetPrice
        { TouchType.OneTouchUp, PaymentType.PayAtHit, 110, 10, 100 },
        { TouchType.OneTouchUp, PaymentType.PayAtExpiry, 110, 10, 100 },
        { TouchType.OneTouchDown, PaymentType.PayAtHit, 90, 10, 100 },
        { TouchType.OneTouchDown, PaymentType.PayAtExpiry, 90, 10, 100 },
        { TouchType.NoTouchUp, PaymentType.PayAtExpiry, 110, 10, 100 },
        { TouchType.NoTouchDown, PaymentType.PayAtExpiry, 90, 10, 100 },
    };
}
