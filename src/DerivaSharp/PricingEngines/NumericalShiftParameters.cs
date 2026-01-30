namespace DerivaSharp.PricingEngines;

public sealed record NumericalShiftParameters
{
    public double AssetPriceShiftFactor { get; init; } = 0.001;

    public double VolatilityShift { get; init; } = 0.001;

    public double InterestRateShift { get; init; } = 0.001;

    public static NumericalShiftParameters Default { get; } = new();
}
