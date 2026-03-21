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
    /// <param name="principalRatio">The ratio of nominal principal prepaid and returned by the note.</param>
    /// <param name="effectiveDate">The date when the note becomes effective.</param>
    /// <param name="expirationDate">The date when the note expires.</param>
    public PhoenixOption(
        double couponRate,
        double initialPrice,
        double knockInPrice,
        IReadOnlyList<double> knockOutPrices,
        IReadOnlyList<double> couponBarrierPrices,
        double upperStrikePrice,
        double lowerStrikePrice,
        DateOnly[] knockOutObservationDates,
        ObservationFrequency knockInObservationFrequency,
        BarrierTouchStatus barrierTouchStatus,
        double principalRatio,
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
            principalRatio,
            effectiveDate,
            expirationDate)
    {
        CouponRate = couponRate;
        CouponBarrierPrices = couponBarrierPrices;
    }

    /// <summary>
    ///     Gets the coupon rate.
    /// </summary>
    /// <value>The annualized coupon rate paid at qualifying observation dates.</value>
    public double CouponRate { get; init; }

    /// <summary>
    ///     Gets the barrier prices for coupon payment at each observation date.
    /// </summary>
    /// <value>A read-only list of barrier prices at or above which a coupon payment is made.</value>
    public IReadOnlyList<double> CouponBarrierPrices { get; init; }

    /// <summary>
    ///     Creates a standard Phoenix option with memory coupon feature.
    /// </summary>
    /// <param name="couponRate">The annualized coupon rate paid at qualifying observation dates.</param>
    /// <param name="initialPrice">The initial price of the underlying asset.</param>
    /// <param name="knockInLevel">The knock-in level as a fraction of the initial price.</param>
    /// <param name="knockOutLevel">The knock-out level as a fraction of the initial price.</param>
    /// <param name="knockOutObservationDates">The dates when knock-out and coupon conditions are checked.</param>
    /// <param name="barrierTouchStatus">One of the enumeration values that specifies the current barrier touch status.</param>
    /// <param name="principalRatio">The ratio of nominal principal prepaid and returned by the note.</param>
    /// <param name="effectiveDate">The date when the note becomes effective.</param>
    /// <param name="expirationDate">The date when the note expires.</param>
    /// <returns>A new <see cref="PhoenixOption" /> configured as a standard Phoenix with memory coupon.</returns>
    public static PhoenixOption CreateStandardPhoenix(
        double couponRate,
        double initialPrice,
        double knockInLevel,
        double knockOutLevel,
        DateOnly[] knockOutObservationDates,
        BarrierTouchStatus barrierTouchStatus,
        double principalRatio,
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
            principalRatio,
            effectiveDate,
            expirationDate);
    }

    /// <summary>
    ///     Creates a fixed coupon note with guaranteed coupon payments.
    /// </summary>
    /// <param name="couponRate">The annualized coupon rate paid at all observation dates.</param>
    /// <param name="initialPrice">The initial price of the underlying asset.</param>
    /// <param name="knockInLevel">The knock-in level as a fraction of the initial price.</param>
    /// <param name="knockOutLevel">The knock-out level as a fraction of the initial price.</param>
    /// <param name="knockOutObservationDates">The dates when knock-out conditions are checked.</param>
    /// <param name="barrierTouchStatus">One of the enumeration values that specifies the current barrier touch status.</param>
    /// <param name="principalRatio">The ratio of nominal principal prepaid and returned by the note.</param>
    /// <param name="effectiveDate">The date when the note becomes effective.</param>
    /// <param name="expirationDate">The date when the note expires.</param>
    /// <returns>A new <see cref="PhoenixOption" /> configured as a fixed coupon note.</returns>
    public static PhoenixOption CreateFixedCouponNote(
        double couponRate,
        double initialPrice,
        double knockInLevel,
        double knockOutLevel,
        DateOnly[] knockOutObservationDates,
        BarrierTouchStatus barrierTouchStatus,
        double principalRatio,
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
            principalRatio,
            effectiveDate,
            expirationDate);
    }

    /// <summary>
    ///     Creates a digital coupon note with binary coupon payments.
    /// </summary>
    /// <param name="couponRate">The annualized coupon rate paid at qualifying observation dates.</param>
    /// <param name="initialPrice">The initial price of the underlying asset.</param>
    /// <param name="knockInLevel">The knock-in level as a fraction of the initial price.</param>
    /// <param name="knockOutLevel">The knock-out level as a fraction of the initial price.</param>
    /// <param name="knockOutObservationDates">The dates when knock-out conditions are checked.</param>
    /// <param name="barrierTouchStatus">One of the enumeration values that specifies the current barrier touch status.</param>
    /// <param name="principalRatio">The ratio of nominal principal prepaid and returned by the note.</param>
    /// <param name="effectiveDate">The date when the note becomes effective.</param>
    /// <param name="expirationDate">The date when the note expires.</param>
    /// <returns>A new <see cref="PhoenixOption" /> configured as a digital coupon note.</returns>
    public static PhoenixOption CreateDigitalCouponNote(
        double couponRate,
        double initialPrice,
        double knockInLevel,
        double knockOutLevel,
        DateOnly[] knockOutObservationDates,
        BarrierTouchStatus barrierTouchStatus,
        double principalRatio,
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
            principalRatio,
            effectiveDate,
            expirationDate);
    }
}
