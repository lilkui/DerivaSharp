namespace DerivaSharp.Instruments;

public abstract record KiAutocallableNote : AutocallableNote
{
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

    public double KnockInPrice { get; init; }

    public ObservationFrequency KnockInObservationFrequency { get; init; }

    public BarrierTouchStatus BarrierTouchStatus { get; init; }
}
