using DerivaSharp.Time;

namespace DerivaSharp.Instruments;

public sealed record BinarySnowballOption : AutocallableNote
{
    public BinarySnowballOption(
        double[] knockOutCouponRates,
        double maturityCouponRate,
        double initialPrice,
        double[] knockOutPrices,
        double upperStrikePrice,
        double lowerStrikePrice,
        DateOnly[] knockOutObservationDates,
        BarrierTouchStatus barrierTouchStatus,
        DateOnly effectiveDate,
        DateOnly expirationDate)
        : base(
            initialPrice,
            knockOutPrices,
            upperStrikePrice,
            lowerStrikePrice,
            knockOutObservationDates,
            effectiveDate,
            expirationDate)
    {
        KnockOutCouponRates = knockOutCouponRates;
        MaturityCouponRate = maturityCouponRate;
        BarrierTouchStatus = barrierTouchStatus;
    }

    public double[] KnockOutCouponRates { get; init; }

    public double MaturityCouponRate { get; init; }

    public BarrierTouchStatus BarrierTouchStatus { get; init; }

    public static BinarySnowballOption Create(
        double knockOutCouponRate,
        double maturityCouponRate,
        double initialPrice,
        double knockOutLevel,
        BarrierTouchStatus barrierTouchStatus,
        DateOnly effectiveDate,
        DateOnly expirationDate)
    {
        DateOnly[] knockOutObservationDates = DateUtils.GetObservationDates(effectiveDate, expirationDate, 0).ToArray();
        int n = knockOutObservationDates.Length;
        return new BinarySnowballOption(
            Enumerable.Repeat(knockOutCouponRate, n).ToArray(),
            maturityCouponRate,
            initialPrice,
            Enumerable.Repeat(initialPrice * knockOutLevel, n).ToArray(),
            initialPrice,
            0,
            knockOutObservationDates,
            barrierTouchStatus,
            effectiveDate,
            expirationDate);
    }
}
