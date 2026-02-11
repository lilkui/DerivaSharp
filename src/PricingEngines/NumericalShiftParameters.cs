namespace DerivaSharp.PricingEngines;

/// <summary>
///     Defines the shift sizes used for numerical differentiation when computing Greeks.
/// </summary>
public sealed record NumericalShiftParameters
{
    /// <summary>
    ///     Gets the relative shift factor for asset price perturbations.
    /// </summary>
    public double AssetPriceShiftFactor { get; init; } = 0.001;

    /// <summary>
    ///     Gets the absolute shift for volatility perturbations.
    /// </summary>
    public double VolatilityShift { get; init; } = 0.001;

    /// <summary>
    ///     Gets the absolute shift for interest rate perturbations.
    /// </summary>
    public double InterestRateShift { get; init; } = 0.001;

    /// <summary>
    ///     Gets the default shift parameters.
    /// </summary>
    public static NumericalShiftParameters Default { get; } = new();
}
