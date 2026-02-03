namespace DerivaSharp.Instruments;

public abstract record AutocallableNote : Option
{
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

    public double InitialPrice { get; init; }

    public double[] KnockOutPrices { get; init; }

    public double UpperStrikePrice { get; init; }

    public double LowerStrikePrice { get; init; }

    public DateOnly[] KnockOutObservationDates { get; init; }
}
