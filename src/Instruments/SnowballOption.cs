namespace DerivaSharp.Instruments;

/// <summary>
///     Represents a snowball autocallable note with knock-in feature and various structural variations.
/// </summary>
public sealed record SnowballOption : KiAutocallableNote
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="SnowballOption" /> class.
    /// </summary>
    /// <param name="knockOutCouponRates">The coupon rates paid at each knock-out observation date.</param>
    /// <param name="maturityCouponRate">The coupon rate paid at maturity if not knocked out.</param>
    /// <param name="initialPrice">The initial price of the underlying asset.</param>
    /// <param name="knockInPrice">The knock-in barrier price.</param>
    /// <param name="knockOutPrices">The barrier prices for early termination at each observation date.</param>
    /// <param name="upperStrikePrice">The upper strike price for payoff calculation.</param>
    /// <param name="lowerStrikePrice">The lower strike price for payoff calculation.</param>
    /// <param name="knockOutObservationDates">The dates when knock-out conditions are checked.</param>
    /// <param name="knockInObservationFrequency">How frequently the knock-in barrier is observed.</param>
    /// <param name="barrierTouchStatus">The current barrier touch status.</param>
    /// <param name="effectiveDate">The date when the note becomes effective.</param>
    /// <param name="expirationDate">The date when the note expires.</param>
    public SnowballOption(
        double[] knockOutCouponRates,
        double maturityCouponRate,
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
    }

    /// <summary>
    ///     Gets the coupon rates paid at each knock-out observation date.
    /// </summary>
    public double[] KnockOutCouponRates { get; init; }

    /// <summary>
    ///     Gets the coupon rate paid at maturity if not knocked out.
    /// </summary>
    public double MaturityCouponRate { get; init; }

    /// <summary>
    ///     Creates a standard snowball with uniform knock-out levels and coupon rates.
    /// </summary>
    public static SnowballOption CreateStandardSnowball(
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
        return new SnowballOption(
            Enumerable.Repeat(couponRate, n).ToArray(),
            couponRate,
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

    /// <summary>
    ///     Creates a step-down snowball with decreasing knock-out levels over time.
    /// </summary>
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
            Enumerable.Repeat(couponRate, knockOutObservationDates.Length).ToArray(),
            couponRate,
            initialPrice,
            initialPrice * knockInLevel,
            knockOutPrices,
            initialPrice,
            0,
            knockOutObservationDates,
            ObservationFrequency.Daily,
            barrierTouchStatus,
            effectiveDate,
            expirationDate);
    }

    /// <summary>
    ///     Creates a both-down snowball with decreasing knock-out levels and coupon rates.
    /// </summary>
    public static SnowballOption CreateBothDownSnowball(
        double knockOutCouponRateStart,
        double knockOutCouponRateStep,
        double initialPrice,
        double knockInLevel,
        double knockOutLevelStart,
        double knockOutLevelStep,
        DateOnly[] knockOutObservationDates,
        BarrierTouchStatus barrierTouchStatus,
        DateOnly effectiveDate,
        DateOnly expirationDate)
    {
        int n = knockOutObservationDates.Length;
        double[] knockOutCouponRates = new double[n];
        double[] knockOutPrices = new double[n];
        for (int i = 0; i < knockOutObservationDates.Length; i++)
        {
            knockOutCouponRates[i] = knockOutCouponRateStart - i * knockOutCouponRateStep;
            knockOutPrices[i] = initialPrice * (knockOutLevelStart - i * knockOutLevelStep);
        }

        return new SnowballOption(
            knockOutCouponRates,
            knockOutCouponRates[^1],
            initialPrice,
            initialPrice * knockInLevel,
            knockOutPrices,
            initialPrice,
            0,
            knockOutObservationDates,
            ObservationFrequency.Daily,
            barrierTouchStatus,
            effectiveDate,
            expirationDate);
    }

    /// <summary>
    ///     Creates a dual-coupon snowball with different knock-out and maturity coupon rates.
    /// </summary>
    public static SnowballOption CreateDualCouponSnowball(
        double knockOutCouponRate,
        double maturityCouponRate,
        double initialPrice,
        double knockInLevel,
        double knockOutLevel,
        DateOnly[] knockOutObservationDates,
        BarrierTouchStatus barrierTouchStatus,
        DateOnly effectiveDate,
        DateOnly expirationDate)
    {
        int n = knockOutObservationDates.Length;
        return new SnowballOption(
            Enumerable.Repeat(knockOutCouponRate, n).ToArray(),
            maturityCouponRate,
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

    /// <summary>
    ///     Creates a parachute snowball with a lower final knock-out level.
    /// </summary>
    public static SnowballOption CreateParachuteSnowball(
        double couponRate,
        double initialPrice,
        double knockInLevel,
        double knockOutLevel,
        double finalKnockOutLevel,
        DateOnly[] knockOutObservationDates,
        BarrierTouchStatus barrierTouchStatus,
        DateOnly effectiveDate,
        DateOnly expirationDate)
    {
        int n = knockOutObservationDates.Length;
        double[] knockOutPrices = Enumerable.Repeat(initialPrice * knockOutLevel, n).ToArray();
        knockOutPrices[^1] = initialPrice * finalKnockOutLevel;
        return new SnowballOption(
            Enumerable.Repeat(couponRate, n).ToArray(),
            couponRate,
            initialPrice,
            initialPrice * knockInLevel,
            knockOutPrices,
            initialPrice,
            0,
            knockOutObservationDates,
            ObservationFrequency.Daily,
            barrierTouchStatus,
            effectiveDate,
            expirationDate);
    }

    /// <summary>
    ///     Creates an out-of-the-money snowball with an upper strike above the initial price.
    /// </summary>
    public static SnowballOption CreateOtmSnowball(
        double couponRate,
        double initialPrice,
        double knockInLevel,
        double knockOutLevel,
        double strikeLevel,
        DateOnly[] knockOutObservationDates,
        BarrierTouchStatus barrierTouchStatus,
        DateOnly effectiveDate,
        DateOnly expirationDate)
    {
        int n = knockOutObservationDates.Length;
        return new SnowballOption(
            Enumerable.Repeat(couponRate, n).ToArray(),
            couponRate,
            initialPrice,
            initialPrice * knockInLevel,
            Enumerable.Repeat(initialPrice * knockOutLevel, n).ToArray(),
            initialPrice * strikeLevel,
            0,
            knockOutObservationDates,
            ObservationFrequency.Daily,
            barrierTouchStatus,
            effectiveDate,
            expirationDate);
    }

    /// <summary>
    ///     Creates a loss-capped snowball with a floor level limiting downside.
    /// </summary>
    public static SnowballOption CreateLossCappedSnowball(
        double couponRate,
        double initialPrice,
        double knockInLevel,
        double knockOutLevel,
        double floorLevel,
        DateOnly[] knockOutObservationDates,
        BarrierTouchStatus barrierTouchStatus,
        DateOnly effectiveDate,
        DateOnly expirationDate)
    {
        int n = knockOutObservationDates.Length;
        return new SnowballOption(
            Enumerable.Repeat(couponRate, n).ToArray(),
            couponRate,
            initialPrice,
            initialPrice * knockInLevel,
            Enumerable.Repeat(initialPrice * knockOutLevel, n).ToArray(),
            initialPrice,
            initialPrice * floorLevel,
            knockOutObservationDates,
            ObservationFrequency.Daily,
            barrierTouchStatus,
            effectiveDate,
            expirationDate);
    }

    /// <summary>
    ///     Creates a European-style snowball with knock-in observed only at expiry.
    /// </summary>
    public static SnowballOption CreateEuropeanSnowball(
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
        return new SnowballOption(
            Enumerable.Repeat(couponRate, n).ToArray(),
            couponRate,
            initialPrice,
            initialPrice * knockInLevel,
            Enumerable.Repeat(initialPrice * knockOutLevel, n).ToArray(),
            initialPrice,
            0,
            knockOutObservationDates,
            ObservationFrequency.AtExpiry,
            barrierTouchStatus,
            effectiveDate,
            expirationDate);
    }
}
