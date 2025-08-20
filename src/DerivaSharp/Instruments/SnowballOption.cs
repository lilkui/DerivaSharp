using DerivaSharp.Time;

namespace DerivaSharp.Instruments;

public sealed record SnowballOption : Option
{
    public SnowballOption(
        double knockOutCouponRate,
        double maturityCouponRate,
        double knockInPrice,
        double knockOutPrice,
        double strikePrice,
        int lockUpMonths,
        BarrierTouchStatus barrierTouchStatus,
        DateOnly effectiveDate,
        DateOnly expirationDate)
        : base(effectiveDate, expirationDate)
    {
        KnockOutCouponRate = knockOutCouponRate;
        MaturityCouponRate = maturityCouponRate;
        KnockInPrice = knockInPrice;
        KnockOutPrice = knockOutPrice;
        StrikePrice = strikePrice;
        KnockOutObservationDates = DateUtils.GetObservationDates(effectiveDate, expirationDate, lockUpMonths).ToArray();
        BarrierTouchStatus = barrierTouchStatus;
    }

    public SnowballOption(
        double knockOutCouponRate,
        double maturityCouponRate,
        double knockInPrice,
        double knockOutPrice,
        double strikePrice,
        DateOnly[] knockOutObservationDates,
        BarrierTouchStatus barrierTouchStatus,
        DateOnly effectiveDate,
        DateOnly expirationDate)
        : base(effectiveDate, expirationDate)
    {
        KnockOutCouponRate = knockOutCouponRate;
        MaturityCouponRate = maturityCouponRate;
        KnockInPrice = knockInPrice;
        KnockOutPrice = knockOutPrice;
        StrikePrice = strikePrice;
        KnockOutObservationDates = knockOutObservationDates;
        BarrierTouchStatus = barrierTouchStatus;
    }

    public double KnockOutCouponRate { get; init; }

    public double MaturityCouponRate { get; init; }

    public double KnockInPrice { get; init; }

    public double KnockOutPrice { get; init; }

    public double StrikePrice { get; init; }

    public DateOnly[] KnockOutObservationDates { get; init; }

    public BarrierTouchStatus BarrierTouchStatus { get; init; }
}
