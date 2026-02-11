using DerivaSharp.Time;

namespace DerivaSharp.Instruments;

/// <summary>
///     Represents a binary snowball autocallable note without a knock-in feature.
/// </summary>
public sealed record BinarySnowballOption : AutocallableNote
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="BinarySnowballOption" /> class.
    /// </summary>
    /// <param name="knockOutCouponRates">The coupon rates paid at each knock-out observation date.</param>
    /// <param name="maturityCouponRate">The coupon rate paid at maturity if not knocked out.</param>
    /// <param name="initialPrice">The initial price of the underlying asset.</param>
    /// <param name="knockOutPrices">The barrier prices for early termination at each observation date.</param>
    /// <param name="upperStrikePrice">The upper strike price for payoff calculation.</param>
    /// <param name="lowerStrikePrice">The lower strike price for payoff calculation.</param>
    /// <param name="knockOutObservationDates">The dates when knock-out conditions are checked.</param>
    /// <param name="barrierTouchStatus">The current barrier touch status.</param>
    /// <param name="effectiveDate">The date when the note becomes effective.</param>
    /// <param name="expirationDate">The date when the note expires.</param>
    public BinarySnowballOption(
        double[] knockOutCouponRates,
        double maturityCouponRate,
        double initialPrice,
        double[] knockOutPrices,
        double upperStrikePrice,
        double lowerStrikePrice,
        DateOnly[] knockOutObservationDates,
        BarrierTouchStatus barrierTouchStatus,
        DateOnly effectiveDate,
        DateOnly expirationDate)
        : base(
            initialPrice,
            knockOutPrices,
            upperStrikePrice,
            lowerStrikePrice,
            knockOutObservationDates,
            effectiveDate,
            expirationDate)
    {
        KnockOutCouponRates = knockOutCouponRates;
        MaturityCouponRate = maturityCouponRate;
        BarrierTouchStatus = barrierTouchStatus;
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
    ///     Gets the current barrier touch status.
    /// </summary>
    public BarrierTouchStatus BarrierTouchStatus { get; init; }

    /// <summary>
    ///     Creates a binary snowball option with uniform knock-out levels and coupon rates.
    /// </summary>
    public static BinarySnowballOption Create(
        double knockOutCouponRate,
        double maturityCouponRate,
        double initialPrice,
        double knockOutLevel,
        BarrierTouchStatus barrierTouchStatus,
        DateOnly effectiveDate,
        DateOnly expirationDate)
    {
        DateOnly[] knockOutObservationDates = DateUtils.GetObservationDates(effectiveDate, expirationDate, 0).ToArray();
        int n = knockOutObservationDates.Length;
        return new BinarySnowballOption(
            Enumerable.Repeat(knockOutCouponRate, n).ToArray(),
            maturityCouponRate,
            initialPrice,
            Enumerable.Repeat(initialPrice * knockOutLevel, n).ToArray(),
            initialPrice,
            0,
            knockOutObservationDates,
            barrierTouchStatus,
            effectiveDate,
            expirationDate);
    }
}
