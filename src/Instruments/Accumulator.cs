namespace DerivaSharp.Instruments;

/// <summary>
///     Represents an accumulator contract that accumulates shares at a strike price with a knock-out barrier.
/// </summary>
public sealed record Accumulator : Option
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="Accumulator" /> class.
    /// </summary>
    /// <param name="strikePrice">The price at which shares are accumulated.</param>
    /// <param name="knockOutPrice">The barrier price that terminates the contract.</param>
    /// <param name="dailyQuantity">The number of shares accumulated daily.</param>
    /// <param name="accelerationFactor">The multiplier applied when the price is below the strike.</param>
    /// <param name="accumulatedQuantity">The quantity already accumulated.</param>
    /// <param name="effectiveDate">The date when the contract becomes effective.</param>
    /// <param name="expirationDate">The date when the contract expires.</param>
    public Accumulator(
        double strikePrice,
        double knockOutPrice,
        double dailyQuantity,
        double accelerationFactor,
        double accumulatedQuantity,
        DateOnly effectiveDate,
        DateOnly expirationDate)
        : base(effectiveDate, expirationDate)
    {
        StrikePrice = strikePrice;
        KnockOutPrice = knockOutPrice;
        DailyQuantity = dailyQuantity;
        AccelerationFactor = accelerationFactor;
        AccumulatedQuantity = accumulatedQuantity;
    }

    /// <summary>
    ///     Gets the price at which shares are accumulated.
    /// </summary>
    public double StrikePrice { get; init; }

    /// <summary>
    ///     Gets the barrier price that terminates the contract.
    /// </summary>
    public double KnockOutPrice { get; init; }

    /// <summary>
    ///     Gets the number of shares accumulated daily.
    /// </summary>
    public double DailyQuantity { get; init; }

    /// <summary>
    ///     Gets the multiplier applied when the price is below the strike.
    /// </summary>
    public double AccelerationFactor { get; init; }

    /// <summary>
    ///     Gets the quantity already accumulated.
    /// </summary>
    public double AccumulatedQuantity { get; init; }
}
