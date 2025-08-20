namespace DerivaSharp.PricingEngines;

public static class NumericalApproximationParameters
{
    public static double AssetPriceShiftFactor { get; set; } = 0.001;

    public static double VolatilityShift { get; set; } = 0.001;

    public static double InterestRateShift { get; set; } = 0.001;
}
