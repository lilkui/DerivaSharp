namespace DerivaSharp.Instruments;

public sealed record Accumulator : Option
{
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

    public double StrikePrice { get; init; }

    public double KnockOutPrice { get; init; }

    public double DailyQuantity { get; init; }

    public double AccelerationFactor { get; init; }

    public double AccumulatedQuantity { get; init; }
}
