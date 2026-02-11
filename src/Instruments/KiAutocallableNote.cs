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
    /// <param name="effectiveDate">The date when the note becomes effective.</param>
    /// <param name="expirationDate">The date when the note expires.</param>
    protected KiAutocallableNote(
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
            knockOutPrices,
            upperStrikePrice,
            lowerStrikePrice,
            knockOutObservationDates,
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
    public double KnockInPrice { get; init; }

    /// <summary>
    ///     Gets how frequently the knock-in barrier is observed.
    /// </summary>
    public ObservationFrequency KnockInObservationFrequency { get; init; }

    /// <summary>
    ///     Gets the current barrier touch status.
    /// </summary>
    public BarrierTouchStatus BarrierTouchStatus { get; init; }
}
