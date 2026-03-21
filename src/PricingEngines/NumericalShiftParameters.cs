namespace DerivaSharp.PricingEngines;

/// <summary>
///     Defines the shift sizes used for numerical differentiation when computing Greeks.
/// </summary>
public sealed record NumericalShiftParameters
{
    /// <summary>
    ///     Gets the relative shift factor for asset price perturbations.
    /// </summary>
    /// <value>The relative shift factor applied to the asset price for numerical differentiation. The default is 0.001.</value>
    public double AssetPriceShiftFactor { get; init; } = 0.001;

    /// <summary>
    ///     Gets the absolute shift for volatility perturbations.
    /// </summary>
    /// <value>The absolute volatility shift applied for numerical differentiation. The default is 0.001.</value>
    public double VolatilityShift { get; init; } = 0.001;

    /// <summary>
    ///     Gets the absolute shift for interest rate perturbations.
    /// </summary>
    /// <value>The absolute interest rate shift applied for numerical differentiation. The default is 0.001.</value>
    public double InterestRateShift { get; init; } = 0.001;

    /// <summary>
    ///     Gets the default shift parameters.
    /// </summary>
    /// <value>A <see cref="NumericalShiftParameters" /> instance with all shifts set to their default values.</value>
    public static NumericalShiftParameters Default { get; } = new();
}
