namespace DerivaSharp.PricingEngines;

/// <summary>
///     Contains the option value and Greeks from a pricing calculation.
/// </summary>
/// <param name="Value">The option value.</param>
/// <param name="Delta">The rate of change of value with respect to the asset price.</param>
/// <param name="Gamma">The rate of change of delta with respect to the asset price.</param>
/// <param name="Speed">The rate of change of gamma with respect to the asset price.</param>
/// <param name="Theta">The rate of change of value with respect to time (per day).</param>
/// <param name="Charm">The rate of change of delta with respect to time (per day).</param>
/// <param name="Color">The rate of change of gamma with respect to time (per day).</param>
/// <param name="Vega">The rate of change of value with respect to volatility (per 1%).</param>
/// <param name="Vanna">The rate of change of delta with respect to volatility (per 1%).</param>
/// <param name="Zomma">The rate of change of gamma with respect to volatility (per 1%).</param>
/// <param name="Rho">The rate of change of value with respect to the risk-free rate (per 1%).</param>
public readonly record struct PricingResult(
    double Value,
    double Delta,
    double Gamma,
    double Speed,
    double Theta,
    double Charm,
    double Color,
    double Vega,
    double Vanna,
    double Zomma,
    double Rho);
