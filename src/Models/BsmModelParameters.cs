using CommunityToolkit.Diagnostics;

namespace DerivaSharp.Models;

/// <summary>
///     Parameters for the Black-Scholes-Merton option pricing model.
/// </summary>
public readonly record struct BsmModelParameters : IModelParameters
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="BsmModelParameters" /> struct.
    /// </summary>
    /// <param name="volatility">The annualized volatility of the underlying asset. Must be greater than or equal to 0.</param>
    /// <param name="riskFreeRate">The annualized risk-free interest rate.</param>
    /// <param name="dividendYield">The annualized dividend yield of the underlying asset.</param>
    /// <exception cref="ArgumentException">Thrown when volatility is less than 0.</exception>
    public BsmModelParameters(double volatility, double riskFreeRate, double dividendYield)
    {
        Guard.IsGreaterThanOrEqualTo(volatility, 0);

        Volatility = volatility;
        RiskFreeRate = riskFreeRate;
        DividendYield = dividendYield;
    }

    /// <summary>
    ///     Gets the annualized volatility of the underlying asset.
    /// </summary>
    /// <value>A non-negative double representing the volatility.</value>
    public double Volatility { get; init; }

    /// <summary>
    ///     Gets the annualized risk-free interest rate.
    /// </summary>
    /// <value>A double representing the risk-free rate.</value>
    public double RiskFreeRate { get; init; }

    /// <summary>
    ///     Gets the annualized dividend yield of the underlying asset.
    /// </summary>
    /// <value>A double representing the dividend yield.</value>
    public double DividendYield { get; init; }
}
