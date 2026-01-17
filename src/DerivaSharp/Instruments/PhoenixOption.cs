namespace DerivaSharp.Instruments;

public sealed record PhoenixOption : AutocallableNote
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
        CouponRate = couponRate;
        CouponBarrierPrices = couponBarrierPrices;
    }

    public double CouponRate { get; init; }

    public double[] CouponBarrierPrices { get; init; }

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

    // FCN
    public static PhoenixOption CreateFixedCouponNote(
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
        double[] couponBarrierPrices = new double[n];

        return new PhoenixOption(
            couponRate,
            initialPrice,
            knockInPrice,
            knockOutPrices,
            couponBarrierPrices,
            initialPrice,
            0,
            knockOutObservationDates,
            ObservationFrequency.AtExpiry,
            barrierTouchStatus,
            effectiveDate,
            expirationDate);
    }

    // DCN
    public static PhoenixOption CreateDigitalCouponNote(
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
            ObservationFrequency.AtExpiry,
            barrierTouchStatus,
            effectiveDate,
            expirationDate);
    }
}
