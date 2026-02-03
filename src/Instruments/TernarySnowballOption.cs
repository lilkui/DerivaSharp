using DerivaSharp.Time;

namespace DerivaSharp.Instruments;

public sealed record TernarySnowballOption : KiAutocallableNote
{
    public TernarySnowballOption(
        double[] knockOutCouponRates,
        double maturityCouponRate,
        double minimalCouponRate,
        double initialPrice,
        double knockInPrice,
        double[] knockOutPrices,
        double upperStrikePrice,
        double lowerStrikePrice,
        DateOnly[] knockOutObservationDates,
        ObservationFrequency knockInObservationFrequency,
        BarrierTouchStatus barrierTouchStatus,
        DateOnly effectiveDate,
        DateOnly expirationDate)
        : base(
            initialPrice,
            knockInPrice,
            knockOutPrices,
            upperStrikePrice,
            lowerStrikePrice,
            knockOutObservationDates,
            knockInObservationFrequency,
            barrierTouchStatus,
            effectiveDate,
            expirationDate)
    {
        KnockOutCouponRates = knockOutCouponRates;
        MaturityCouponRate = maturityCouponRate;
        MinimalCouponRate = minimalCouponRate;
    }

    public double[] KnockOutCouponRates { get; init; }

    public double MaturityCouponRate { get; init; }

    public double MinimalCouponRate { get; init; }

    public static TernarySnowballOption Create(
        double knockOutCouponRate,
        double maturityCouponRate,
        double minimalCouponRate,
        double initialPrice,
        double knockInLevel,
        double knockOutLevel,
        BarrierTouchStatus barrierTouchStatus,
        DateOnly effectiveDate,
        DateOnly expirationDate)
    {
        DateOnly[] knockOutObservationDates = DateUtils.GetObservationDates(effectiveDate, expirationDate, 0).ToArray();
        int n = knockOutObservationDates.Length;
        return new TernarySnowballOption(
            Enumerable.Repeat(knockOutCouponRate, n).ToArray(),
            maturityCouponRate,
            minimalCouponRate,
            initialPrice,
            initialPrice * knockInLevel,
            Enumerable.Repeat(initialPrice * knockOutLevel, n).ToArray(),
            initialPrice,
            0,
            knockOutObservationDates,
            ObservationFrequency.Daily,
            barrierTouchStatus,
            effectiveDate,
            expirationDate);
    }
}
