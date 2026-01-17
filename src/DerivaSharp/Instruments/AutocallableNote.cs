namespace DerivaSharp.Instruments;

public abstract record AutocallableNote : Option
{
    protected AutocallableNote(
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
        : base(effectiveDate, expirationDate)
    {
        InitialPrice = initialPrice;
        KnockInPrice = knockInPrice;
        KnockOutPrices = knockOutPrices;
        UpperStrikePrice = upperStrikePrice;
        LowerStrikePrice = lowerStrikePrice;
        KnockOutObservationDates = knockOutObservationDates;
        KnockInObservationFrequency = knockInObservationFrequency;
        BarrierTouchStatus = barrierTouchStatus;
    }

    public double InitialPrice { get; init; }

    public double KnockInPrice { get; init; }

    public double[] KnockOutPrices { get; init; }

    public double UpperStrikePrice { get; init; }

    public double LowerStrikePrice { get; init; }

    public DateOnly[] KnockOutObservationDates { get; init; }

    public ObservationFrequency KnockInObservationFrequency { get; init; }

    public BarrierTouchStatus BarrierTouchStatus { get; init; }
}
