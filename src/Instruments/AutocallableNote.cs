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
    /// <param name="effectiveDate">The date when the note becomes effective.</param>
    /// <param name="expirationDate">The date when the note expires.</param>
    protected AutocallableNote(
        double initialPrice,
        double[] knockOutPrices,
        double upperStrikePrice,
        double lowerStrikePrice,
        DateOnly[] knockOutObservationDates,
        DateOnly effectiveDate,
        DateOnly expirationDate)
        : base(effectiveDate, expirationDate)
    {
        InitialPrice = initialPrice;
        KnockOutPrices = knockOutPrices;
        UpperStrikePrice = upperStrikePrice;
        LowerStrikePrice = lowerStrikePrice;
        KnockOutObservationDates = knockOutObservationDates;
    }

    /// <summary>
    ///     Gets the initial price of the underlying asset.
    /// </summary>
    public double InitialPrice { get; init; }

    /// <summary>
    ///     Gets the barrier prices for early termination at each observation date.
    /// </summary>
    public double[] KnockOutPrices { get; init; }

    /// <summary>
    ///     Gets the upper strike price for payoff calculation.
    /// </summary>
    public double UpperStrikePrice { get; init; }

    /// <summary>
    ///     Gets the lower strike price for payoff calculation.
    /// </summary>
    public double LowerStrikePrice { get; init; }

    /// <summary>
    ///     Gets the dates when knock-out conditions are checked.
    /// </summary>
    public DateOnly[] KnockOutObservationDates { get; init; }
}
