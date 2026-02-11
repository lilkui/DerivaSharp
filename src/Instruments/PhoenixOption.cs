namespace DerivaSharp.Instruments;

/// <summary>
///     Represents a Phoenix autocallable note with memory coupon features.
/// </summary>
public sealed record PhoenixOption : KiAutocallableNote
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="PhoenixOption" /> class.
    /// </summary>
    /// <param name="couponRate">The coupon rate.</param>
    /// <param name="initialPrice">The initial price of the underlying asset.</param>
    /// <param name="knockInPrice">The knock-in barrier price.</param>
    /// <param name="knockOutPrices">The barrier prices for early termination at each observation date.</param>
    /// <param name="couponBarrierPrices">The barrier prices for coupon payment at each observation date.</param>
    /// <param name="upperStrikePrice">The upper strike price for payoff calculation.</param>
    /// <param name="lowerStrikePrice">The lower strike price for payoff calculation.</param>
    /// <param name="knockOutObservationDates">The dates when knock-out conditions are checked.</param>
    /// <param name="knockInObservationFrequency">How frequently the knock-in barrier is observed.</param>
    /// <param name="barrierTouchStatus">The current barrier touch status.</param>
    /// <param name="effectiveDate">The date when the note becomes effective.</param>
    /// <param name="expirationDate">The date when the note expires.</param>
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

    /// <summary>
    ///     Gets the coupon rate.
    /// </summary>
    public double CouponRate { get; init; }

    /// <summary>
    ///     Gets the barrier prices for coupon payment at each observation date.
    /// </summary>
    public double[] CouponBarrierPrices { get; init; }

    /// <summary>
    ///     Creates a standard Phoenix option with memory coupon feature.
    /// </summary>
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

    /// <summary>
    ///     Creates a fixed coupon note with guaranteed coupon payments.
    /// </summary>
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

    /// <summary>
    ///     Creates a digital coupon note with binary coupon payments.
    /// </summary>
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
