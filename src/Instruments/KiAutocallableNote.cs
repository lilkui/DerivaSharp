namespace DerivaSharp.Instruments;

/// <summary>
///     Base class for autocallable notes with a knock-in barrier feature.
/// </summary>
public abstract record KiAutocallableNote : AutocallableNote
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="KiAutocallableNote" /> class.
    /// </summary>
    /// <param name="initialPrice">The initial price of the underlying asset.</param>
    /// <param name="knockInPrice">The knock-in barrier price.</param>
    /// <param name="knockOutPrices">The barrier prices for early termination at each observation date.</param>
    /// <param name="upperStrikePrice">The upper strike price for payoff calculation.</param>
    /// <param name="lowerStrikePrice">The lower strike price for payoff calculation.</param>
    /// <param name="knockOutObservationDates">The dates when knock-out conditions are checked.</param>
    /// <param name="knockInObservationFrequency">How frequently the knock-in barrier is observed.</param>
    /// <param name="barrierTouchStatus">The current barrier touch status.</param>
    /// <param name="principalRatio">The ratio of nominal principal prepaid and returned by the note.</param>
    /// <param name="effectiveDate">The date when the note becomes effective.</param>
    /// <param name="expirationDate">The date when the note expires.</param>
    protected KiAutocallableNote(
        double initialPrice,
        double knockInPrice,
        IReadOnlyList<double> knockOutPrices,
        double upperStrikePrice,
        double lowerStrikePrice,
        IReadOnlyList<DateOnly> knockOutObservationDates,
        ObservationFrequency knockInObservationFrequency,
        BarrierTouchStatus barrierTouchStatus,
        double principalRatio,
        DateOnly effectiveDate,
        DateOnly expirationDate)
        : base(
            initialPrice,
            knockOutPrices,
            upperStrikePrice,
            lowerStrikePrice,
            knockOutObservationDates,
            principalRatio,
            effectiveDate,
            expirationDate)
    {
        KnockInPrice = knockInPrice;
        KnockInObservationFrequency = knockInObservationFrequency;
        BarrierTouchStatus = barrierTouchStatus;
    }

    /// <summary>
    ///     Gets the knock-in barrier price.
    /// </summary>
    /// <value>The barrier price at which the knock-in event is triggered.</value>
    public double KnockInPrice { get; init; }

    /// <summary>
    ///     Gets how frequently the knock-in barrier is observed.
    /// </summary>
    /// <value>One of the <see cref="ObservationFrequency" /> values that specifies how often the knock-in barrier is observed.</value>
    public ObservationFrequency KnockInObservationFrequency { get; init; }

    /// <summary>
    ///     Gets the current barrier touch status.
    /// </summary>
    /// <value>One of the <see cref="BarrierTouchStatus" /> values that indicates whether the barrier has been touched.</value>
    public BarrierTouchStatus BarrierTouchStatus { get; init; }
}
