using CommunityToolkit.Diagnostics;

namespace DerivaSharp.Instruments;

public sealed record SingleTouchOption : CashOrNothingBarrierOption
{
    public SingleTouchOption(
        TouchType touchType,
        PaymentType rebatePaymentType,
        double strikePrice,
        double barrierPrice,
        double rebate,
        DateOnly effectiveDate,
        DateOnly expirationDate)
        : base(
            touchType switch
            {
                TouchType.OneTouchUp => BarrierType.UpAndIn,
                TouchType.OneTouchDown => BarrierType.DownAndIn,
                TouchType.NoTouchUp => BarrierType.UpAndOut,
                TouchType.NoTouchDown => BarrierType.DownAndOut,
                _ => ThrowHelper.ThrowArgumentOutOfRangeException<BarrierType>(nameof(touchType), "Invalid touch type"),
            },
            rebatePaymentType,
            null,
            strikePrice,
            barrierPrice,
            rebate,
            0,
            effectiveDate,
            expirationDate) =>
        TouchType = touchType;

    public TouchType TouchType { get; init; }
}
