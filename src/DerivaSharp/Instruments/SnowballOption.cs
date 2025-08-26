namespace DerivaSharp.Instruments;

public sealed record SnowballOption : Option
{
    public SnowballOption(
        double knockOutCouponRate,
        double maturityCouponRate,
        double initialPrice,
        double knockInPrice,
        double[] knockOutPrices,
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
        KnockOutPrices = knockOutPrices;
        InitialPrice = initialPrice;
        StrikePrice = strikePrice;
        KnockOutObservationDates = knockOutObservationDates;
        BarrierTouchStatus = barrierTouchStatus;
    }

    public double KnockOutCouponRate { get; init; }

    public double MaturityCouponRate { get; init; }

    public double InitialPrice { get; init; }

    public double KnockInPrice { get; init; }

    public double[] KnockOutPrices { get; init; }

    public double StrikePrice { get; init; }

    public DateOnly[] KnockOutObservationDates { get; init; }

    public BarrierTouchStatus BarrierTouchStatus { get; init; }

    public static SnowballOption CreateStandardSnowball(
        double couponRate,
        double initialPrice,
        double knockInLevel,
        double knockOutLevel,
        DateOnly[] knockOutObservationDates,
        BarrierTouchStatus barrierTouchStatus,
        DateOnly effectiveDate,
        DateOnly expirationDate) =>
        new(
            couponRate,
            couponRate,
            initialPrice,
            initialPrice * knockInLevel,
            Enumerable.Repeat(initialPrice * knockOutLevel, knockOutObservationDates.Length).ToArray(),
            initialPrice,
            knockOutObservationDates,
            barrierTouchStatus,
            effectiveDate,
            expirationDate);

    public static SnowballOption CreateStepDownSnowball(
        double couponRate,
        double initialPrice,
        double knockInLevel,
        double knockOutLevelStart,
        double knockOutLevelStep,
        DateOnly[] knockOutObservationDates,
        BarrierTouchStatus barrierTouchStatus,
        DateOnly effectiveDate,
        DateOnly expirationDate)
    {
        double[] knockOutPrices = knockOutObservationDates
            .Select((_, i) => initialPrice * (knockOutLevelStart - i * knockOutLevelStep))
            .ToArray();
        return new SnowballOption(
            couponRate,
            couponRate,
            initialPrice,
            initialPrice * knockInLevel,
            knockOutPrices,
            initialPrice,
            knockOutObservationDates,
            barrierTouchStatus,
            effectiveDate,
            expirationDate);
    }
}
