namespace DerivaSharp.Instruments;

public sealed record PhoenixOption : Option
{
    public PhoenixOption(
        double couponRate,
        double initialPrice,
        double knockInPrice,
        double[] knockOutPrices,
        double[] couponBarrierPrices,
        double upperStrikePrice,
        double lowerStrikePrice,
        DateOnly[] knockOutObservationDates,
        ObservationFrequency knockInObservationFrequency,
        BarrierTouchStatus barrierTouchStatus,
        DateOnly effectiveDate,
        DateOnly expirationDate)
        : base(effectiveDate, expirationDate)
    {
        CouponRate = couponRate;
        InitialPrice = initialPrice;
        KnockInPrice = knockInPrice;
        KnockOutPrices = knockOutPrices;
        CouponBarrierPrices = couponBarrierPrices;
        UpperStrikePrice = upperStrikePrice;
        LowerStrikePrice = lowerStrikePrice;
        KnockOutObservationDates = knockOutObservationDates;
        KnockInObservationFrequency = knockInObservationFrequency;
        BarrierTouchStatus = barrierTouchStatus;
    }

    public double CouponRate { get; init; }

    public double InitialPrice { get; init; }

    public double KnockInPrice { get; init; }

    public double[] KnockOutPrices { get; init; }

    public double[] CouponBarrierPrices { get; init; }

    public double UpperStrikePrice { get; init; }

    public double LowerStrikePrice { get; init; }

    public DateOnly[] KnockOutObservationDates { get; init; }

    public ObservationFrequency KnockInObservationFrequency { get; init; }

    public BarrierTouchStatus BarrierTouchStatus { get; init; }

    public static PhoenixOption CreateStandardPhoenix(
        double couponRate,
        double initialPrice,
        double knockInLevel,
        double knockOutLevel,
        DateOnly[] knockOutObservationDates,
        BarrierTouchStatus barrierTouchStatus,
        DateOnly effectiveDate,
        DateOnly expirationDate)
    {
        int n = knockOutObservationDates.Length;
        double knockInPrice = initialPrice * knockInLevel;
        double[] knockOutPrices = Enumerable.Repeat(initialPrice * knockOutLevel, n).ToArray();
        double[] couponBarrierPrices = Enumerable.Repeat(initialPrice * knockInLevel, n).ToArray();

        return new PhoenixOption(
            couponRate,
            initialPrice,
            knockInPrice,
            knockOutPrices,
            couponBarrierPrices,
            initialPrice,
            0,
            knockOutObservationDates,
            ObservationFrequency.Daily,
            barrierTouchStatus,
            effectiveDate,
            expirationDate);
    }
}
