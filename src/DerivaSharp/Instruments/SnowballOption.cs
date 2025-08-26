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
        double knockInPrice,
        double knockOutPrice,
        DateOnly[] knockOutObservationDates,
        BarrierTouchStatus barrierTouchStatus,
        DateOnly effectiveDate,
        DateOnly expirationDate) =>
        new(
            couponRate,
            couponRate,
            initialPrice,
            knockInPrice,
            Enumerable.Repeat(knockOutPrice, knockOutObservationDates.Length).ToArray(),
            initialPrice,
            knockOutObservationDates,
            barrierTouchStatus,
            effectiveDate,
            expirationDate);
}
