using CommunityToolkit.Diagnostics;

namespace DerivaSharp.Instruments;

/// <summary>
///     Base class for autocallable structured notes that may terminate early if knock-out conditions are met.
/// </summary>
public abstract record AutocallableNote : Option
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="AutocallableNote" /> class.
    /// </summary>
    /// <param name="initialPrice">The initial price of the underlying asset.</param>
    /// <param name="knockOutPrices">The barrier prices for early termination at each observation date.</param>
    /// <param name="upperStrikePrice">The upper strike price for payoff calculation.</param>
    /// <param name="lowerStrikePrice">The lower strike price for payoff calculation.</param>
    /// <param name="knockOutObservationDates">The dates when knock-out conditions are checked.</param>
    /// <param name="principalRatio">The ratio of nominal principal prepaid and returned by the note.</param>
    /// <param name="effectiveDate">The date when the note becomes effective.</param>
    /// <param name="expirationDate">The date when the note expires.</param>
    protected AutocallableNote(
        double initialPrice,
        IReadOnlyList<double> knockOutPrices,
        double upperStrikePrice,
        double lowerStrikePrice,
        IReadOnlyList<DateOnly> knockOutObservationDates,
        double principalRatio,
        DateOnly effectiveDate,
        DateOnly expirationDate)
        : base(effectiveDate, expirationDate)
    {
        Guard.IsGreaterThanOrEqualTo(principalRatio, 0.0);

        InitialPrice = initialPrice;
        KnockOutPrices = knockOutPrices;
        UpperStrikePrice = upperStrikePrice;
        LowerStrikePrice = lowerStrikePrice;
        KnockOutObservationDates = knockOutObservationDates;
        PrincipalRatio = principalRatio;
    }

    /// <summary>
    ///     Gets the initial price of the underlying asset.
    /// </summary>
    /// <value>The initial price of the underlying asset, used as the reference for barrier calculations.</value>
    public double InitialPrice { get; init; }

    /// <summary>
    ///     Gets the barrier prices for early termination at each observation date.
    /// </summary>
    /// <value>A read-only list of barrier prices, one for each knock-out observation date.</value>
    public IReadOnlyList<double> KnockOutPrices { get; init; }

    /// <summary>
    ///     Gets the upper strike price for payoff calculation.
    /// </summary>
    /// <value>The upper strike price used in payoff calculation.</value>
    public double UpperStrikePrice { get; init; }

    /// <summary>
    ///     Gets the lower strike price for payoff calculation.
    /// </summary>
    /// <value>The lower strike price used in payoff calculation.</value>
    public double LowerStrikePrice { get; init; }

    /// <summary>
    ///     Gets the dates when knock-out conditions are checked.
    /// </summary>
    /// <value>A read-only list of dates on which knock-out conditions are evaluated.</value>
    public IReadOnlyList<DateOnly> KnockOutObservationDates { get; init; }

    /// <summary>
    ///     Gets the ratio of nominal principal prepaid and returned by the note.
    /// </summary>
    /// <value>The principal ratio; a value of 1.0 represents full principal repayment.</value>
    public double PrincipalRatio { get; init; }
}
