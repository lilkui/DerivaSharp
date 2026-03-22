using DerivaSharp.Time;

namespace DerivaSharp.Instruments;

/// <summary>
///     Represents a ternary snowball option with three possible payoff outcomes.
/// </summary>
public sealed record TernarySnowballOption : KiAutocallableNote
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="TernarySnowballOption" /> class.
    /// </summary>
    /// <param name="knockOutCouponRates">The coupon rates paid at each knock-out observation date.</param>
    /// <param name="maturityCouponRate">The coupon rate paid at maturity if not knocked out.</param>
    /// <param name="minimalCouponRate">The minimal coupon rate guaranteed.</param>
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
    public TernarySnowballOption(
        IReadOnlyList<double> knockOutCouponRates,
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
        MinimalCouponRate = minimalCouponRate;
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
    ///     Gets the minimal coupon rate guaranteed.
    /// </summary>
    /// <value>The minimum annualized coupon rate guaranteed regardless of the underlying's performance.</value>
    public double MinimalCouponRate { get; init; }

    /// <summary>
    ///     Creates a ternary snowball option with uniform knock-out levels and coupon rates.
    /// </summary>
    /// <param name="knockOutCouponRate">The annualized coupon rate paid at knock-out observation dates.</param>
    /// <param name="maturityCouponRate">The annualized coupon rate paid at maturity if the note is not knocked out.</param>
    /// <param name="minimalCouponRate">The minimum annualized coupon rate guaranteed.</param>
    /// <param name="initialPrice">The initial price of the underlying asset.</param>
    /// <param name="knockInLevel">The knock-in level as a fraction of the initial price.</param>
    /// <param name="knockOutLevel">The knock-out level as a fraction of the initial price.</param>
    /// <param name="barrierTouchStatus">One of the <see cref="BarrierTouchStatus" /> enumeration values that specifies the current barrier touch status.</param>
    /// <param name="principalRatio">The ratio of nominal principal prepaid and returned by the note.</param>
    /// <param name="effectiveDate">The date when the note becomes effective.</param>
    /// <param name="expirationDate">The date when the note expires.</param>
    /// <returns>A new <see cref="TernarySnowballOption" /> with uniform knock-out levels and coupon rates.</returns>
    public static TernarySnowballOption Create(
        double knockOutCouponRate,
        double maturityCouponRate,
        double minimalCouponRate,
        double initialPrice,
        double knockInLevel,
        double knockOutLevel,
        BarrierTouchStatus barrierTouchStatus,
        double principalRatio,
        DateOnly effectiveDate,
        DateOnly expirationDate)
    {
        DateOnly[] knockOutObservationDates = SseCalendar.Shared.GetMonthlyObservationDates(effectiveDate, expirationDate, 0).ToArray();
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
            principalRatio,
            effectiveDate,
            expirationDate);
    }
}
