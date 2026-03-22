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
    /// <param name="knockInObservationFrequency">One of the <see cref="ObservationFrequency" /> enumeration values that specifies how frequently the knock-in barrier is observed.</param>
    /// <param name="barrierTouchStatus">One of the <see cref="BarrierTouchStatus" /> enumeration values that indicates the current barrier touch status.</param>
    /// <param name="principalRatio">The ratio of nominal principal prepaid and returned by the note.</param>
    /// <param name="effectiveDate">The date when the note becomes effective.</param>
    /// <param name="expirationDate">The date when the note expires.</param>
    public SnowballOption(
        IReadOnlyList<double> knockOutCouponRates,
        double maturityCouponRate,
        double initialPrice,
        double knockInPrice,
        double[] knockOutPrices,
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
        KnockOutCouponRates = knockOutCouponRates;
        MaturityCouponRate = maturityCouponRate;
    }

    /// <summary>
    ///     Gets the coupon rates paid at each knock-out observation date.
    /// </summary>
    /// <value>A read-only list of annualized coupon rates, one for each knock-out observation date.</value>
    public IReadOnlyList<double> KnockOutCouponRates { get; init; }

    /// <summary>
    ///     Gets the coupon rate paid at maturity if not knocked out.
    /// </summary>
    /// <value>The annualized coupon rate paid at maturity if the note is not knocked out.</value>
    public double MaturityCouponRate { get; init; }

    /// <summary>
    ///     Creates a standard snowball with uniform knock-out levels and coupon rates.
    /// </summary>
    /// <param name="couponRate">The annualized coupon rate paid at knock-out and at maturity.</param>
    /// <param name="initialPrice">The initial price of the underlying asset.</param>
    /// <param name="knockInLevel">The knock-in level as a fraction of the initial price.</param>
    /// <param name="knockOutLevel">The knock-out level as a fraction of the initial price.</param>
    /// <param name="knockOutObservationDates">The dates when knock-out conditions are checked.</param>
    /// <param name="barrierTouchStatus">One of the <see cref="BarrierTouchStatus" /> enumeration values that specifies the current barrier touch status.</param>
    /// <param name="principalRatio">The ratio of nominal principal prepaid and returned by the note.</param>
    /// <param name="effectiveDate">The date when the note becomes effective.</param>
    /// <param name="expirationDate">The date when the note expires.</param>
    /// <returns>A new <see cref="SnowballOption" /> configured as a standard snowball.</returns>
    public static SnowballOption CreateStandardSnowball(
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
            principalRatio,
            effectiveDate,
            expirationDate);
    }

    /// <summary>
    ///     Creates a step-down snowball with decreasing knock-out levels over time.
    /// </summary>
    /// <param name="couponRate">The annualized coupon rate paid at all observation dates.</param>
    /// <param name="initialPrice">The initial price of the underlying asset.</param>
    /// <param name="knockInLevel">The knock-in level as a fraction of the initial price.</param>
    /// <param name="knockOutLevelStart">The initial knock-out level as a fraction of the initial price.</param>
    /// <param name="knockOutLevelStep">The amount by which the knock-out level decreases at each observation date.</param>
    /// <param name="knockOutObservationDates">The dates when knock-out conditions are checked.</param>
    /// <param name="barrierTouchStatus">One of the <see cref="BarrierTouchStatus" /> enumeration values that specifies the current barrier touch status.</param>
    /// <param name="principalRatio">The ratio of nominal principal prepaid and returned by the note.</param>
    /// <param name="effectiveDate">The date when the note becomes effective.</param>
    /// <param name="expirationDate">The date when the note expires.</param>
    /// <returns>A new <see cref="SnowballOption" /> configured as a step-down snowball.</returns>
    public static SnowballOption CreateStepDownSnowball(
        double couponRate,
        double initialPrice,
        double knockInLevel,
        double knockOutLevelStart,
        double knockOutLevelStep,
        DateOnly[] knockOutObservationDates,
        BarrierTouchStatus barrierTouchStatus,
        double principalRatio,
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
            principalRatio,
            effectiveDate,
            expirationDate);
    }

    /// <summary>
    ///     Creates a both-down snowball with decreasing knock-out levels and coupon rates.
    /// </summary>
    /// <param name="knockOutCouponRateStart">The initial annualized knock-out coupon rate.</param>
    /// <param name="knockOutCouponRateStep">The amount by which the knock-out coupon rate decreases at each observation date.</param>
    /// <param name="initialPrice">The initial price of the underlying asset.</param>
    /// <param name="knockInLevel">The knock-in level as a fraction of the initial price.</param>
    /// <param name="knockOutLevelStart">The initial knock-out level as a fraction of the initial price.</param>
    /// <param name="knockOutLevelStep">The amount by which the knock-out level decreases at each observation date.</param>
    /// <param name="knockOutObservationDates">The dates when knock-out conditions are checked.</param>
    /// <param name="barrierTouchStatus">One of the <see cref="BarrierTouchStatus" /> enumeration values that specifies the current barrier touch status.</param>
    /// <param name="principalRatio">The ratio of nominal principal prepaid and returned by the note.</param>
    /// <param name="effectiveDate">The date when the note becomes effective.</param>
    /// <param name="expirationDate">The date when the note expires.</param>
    /// <returns>A new <see cref="SnowballOption" /> configured as a both-down snowball.</returns>
    public static SnowballOption CreateBothDownSnowball(
        double knockOutCouponRateStart,
        double knockOutCouponRateStep,
        double initialPrice,
        double knockInLevel,
        double knockOutLevelStart,
        double knockOutLevelStep,
        DateOnly[] knockOutObservationDates,
        BarrierTouchStatus barrierTouchStatus,
        double principalRatio,
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
            principalRatio,
            effectiveDate,
            expirationDate);
    }

    /// <summary>
    ///     Creates a dual-coupon snowball with different knock-out and maturity coupon rates.
    /// </summary>
    /// <param name="knockOutCouponRate">The annualized coupon rate paid at knock-out observation dates.</param>
    /// <param name="maturityCouponRate">The annualized coupon rate paid at maturity.</param>
    /// <param name="initialPrice">The initial price of the underlying asset.</param>
    /// <param name="knockInLevel">The knock-in level as a fraction of the initial price.</param>
    /// <param name="knockOutLevel">The knock-out level as a fraction of the initial price.</param>
    /// <param name="knockOutObservationDates">The dates when knock-out conditions are checked.</param>
    /// <param name="barrierTouchStatus">One of the <see cref="BarrierTouchStatus" /> enumeration values that specifies the current barrier touch status.</param>
    /// <param name="principalRatio">The ratio of nominal principal prepaid and returned by the note.</param>
    /// <param name="effectiveDate">The date when the note becomes effective.</param>
    /// <param name="expirationDate">The date when the note expires.</param>
    /// <returns>A new <see cref="SnowballOption" /> configured as a dual-coupon snowball.</returns>
    public static SnowballOption CreateDualCouponSnowball(
        double knockOutCouponRate,
        double maturityCouponRate,
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
            principalRatio,
            effectiveDate,
            expirationDate);
    }

    /// <summary>
    ///     Creates a parachute snowball with a lower final knock-out level.
    /// </summary>
    /// <param name="couponRate">The annualized coupon rate paid at knock-out and at maturity.</param>
    /// <param name="initialPrice">The initial price of the underlying asset.</param>
    /// <param name="knockInLevel">The knock-in level as a fraction of the initial price.</param>
    /// <param name="knockOutLevel">The knock-out level as a fraction of the initial price.</param>
    /// <param name="finalKnockOutLevel">The knock-out level applied at the final observation date as a fraction of the initial price.</param>
    /// <param name="knockOutObservationDates">The dates when knock-out conditions are checked.</param>
    /// <param name="barrierTouchStatus">One of the <see cref="BarrierTouchStatus" /> enumeration values that specifies the current barrier touch status.</param>
    /// <param name="principalRatio">The ratio of nominal principal prepaid and returned by the note.</param>
    /// <param name="effectiveDate">The date when the note becomes effective.</param>
    /// <param name="expirationDate">The date when the note expires.</param>
    /// <returns>A new <see cref="SnowballOption" /> configured as a parachute snowball.</returns>
    public static SnowballOption CreateParachuteSnowball(
        double couponRate,
        double initialPrice,
        double knockInLevel,
        double knockOutLevel,
        double finalKnockOutLevel,
        DateOnly[] knockOutObservationDates,
        BarrierTouchStatus barrierTouchStatus,
        double principalRatio,
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
            principalRatio,
            effectiveDate,
            expirationDate);
    }

    /// <summary>
    ///     Creates an out-of-the-money snowball with an upper strike above the initial price.
    /// </summary>
    /// <param name="couponRate">The annualized coupon rate paid at knock-out and at maturity.</param>
    /// <param name="initialPrice">The initial price of the underlying asset.</param>
    /// <param name="knockInLevel">The knock-in level as a fraction of the initial price.</param>
    /// <param name="knockOutLevel">The knock-out level as a fraction of the initial price.</param>
    /// <param name="strikeLevel">The upper strike level as a fraction of the initial price.</param>
    /// <param name="knockOutObservationDates">The dates when knock-out conditions are checked.</param>
    /// <param name="barrierTouchStatus">One of the <see cref="BarrierTouchStatus" /> enumeration values that specifies the current barrier touch status.</param>
    /// <param name="principalRatio">The ratio of nominal principal prepaid and returned by the note.</param>
    /// <param name="effectiveDate">The date when the note becomes effective.</param>
    /// <param name="expirationDate">The date when the note expires.</param>
    /// <returns>A new <see cref="SnowballOption" /> configured as an OTM snowball.</returns>
    public static SnowballOption CreateOtmSnowball(
        double couponRate,
        double initialPrice,
        double knockInLevel,
        double knockOutLevel,
        double strikeLevel,
        DateOnly[] knockOutObservationDates,
        BarrierTouchStatus barrierTouchStatus,
        double principalRatio,
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
            principalRatio,
            effectiveDate,
            expirationDate);
    }

    /// <summary>
    ///     Creates a loss-capped snowball with a floor level limiting downside.
    /// </summary>
    /// <param name="couponRate">The annualized coupon rate paid at knock-out and at maturity.</param>
    /// <param name="initialPrice">The initial price of the underlying asset.</param>
    /// <param name="knockInLevel">The knock-in level as a fraction of the initial price.</param>
    /// <param name="knockOutLevel">The knock-out level as a fraction of the initial price.</param>
    /// <param name="floorLevel">The lower strike floor level as a fraction of the initial price.</param>
    /// <param name="knockOutObservationDates">The dates when knock-out conditions are checked.</param>
    /// <param name="barrierTouchStatus">One of the <see cref="BarrierTouchStatus" /> enumeration values that specifies the current barrier touch status.</param>
    /// <param name="principalRatio">The ratio of nominal principal prepaid and returned by the note.</param>
    /// <param name="effectiveDate">The date when the note becomes effective.</param>
    /// <param name="expirationDate">The date when the note expires.</param>
    /// <returns>A new <see cref="SnowballOption" /> configured as a loss-capped snowball.</returns>
    public static SnowballOption CreateLossCappedSnowball(
        double couponRate,
        double initialPrice,
        double knockInLevel,
        double knockOutLevel,
        double floorLevel,
        DateOnly[] knockOutObservationDates,
        BarrierTouchStatus barrierTouchStatus,
        double principalRatio,
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
            principalRatio,
            effectiveDate,
            expirationDate);
    }

    /// <summary>
    ///     Creates a European-style snowball with knock-in observed only at expiry.
    /// </summary>
    /// <param name="couponRate">The annualized coupon rate paid at knock-out and at maturity.</param>
    /// <param name="initialPrice">The initial price of the underlying asset.</param>
    /// <param name="knockInLevel">The knock-in level as a fraction of the initial price.</param>
    /// <param name="knockOutLevel">The knock-out level as a fraction of the initial price.</param>
    /// <param name="knockOutObservationDates">The dates when knock-out conditions are checked.</param>
    /// <param name="barrierTouchStatus">One of the <see cref="BarrierTouchStatus" /> enumeration values that specifies the current barrier touch status.</param>
    /// <param name="principalRatio">The ratio of nominal principal prepaid and returned by the note.</param>
    /// <param name="effectiveDate">The date when the note becomes effective.</param>
    /// <param name="expirationDate">The date when the note expires.</param>
    /// <returns>A new <see cref="SnowballOption" /> configured as a European-style snowball.</returns>
    public static SnowballOption CreateEuropeanSnowball(
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
            principalRatio,
            effectiveDate,
            expirationDate);
    }
}
