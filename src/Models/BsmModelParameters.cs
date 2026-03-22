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
    /// <param name="volatility">The annualized volatility of the underlying asset. Must be greater than 0.</param>
    /// <param name="riskFreeRate">The annualized risk-free interest rate.</param>
    /// <param name="dividendYield">The annualized dividend yield of the underlying asset.</param>
    /// <exception cref="ArgumentException">Volatility is less than or equal to 0.</exception>
    public BsmModelParameters(double volatility, double riskFreeRate, double dividendYield)
    {
        Guard.IsGreaterThan(volatility, 0);

        Volatility = volatility;
        RiskFreeRate = riskFreeRate;
        DividendYield = dividendYield;
    }

    /// <summary>
    ///     Gets the annualized volatility of the underlying asset.
    /// </summary>
    /// <value>The annualized volatility of the underlying asset, which must be positive.</value>
    public double Volatility { get; init; }

    /// <summary>
    ///     Gets the annualized risk-free interest rate.
    /// </summary>
    /// <value>The annualized risk-free interest rate.</value>
    public double RiskFreeRate { get; init; }

    /// <summary>
    ///     Gets the annualized dividend yield of the underlying asset.
    /// </summary>
    /// <value>The annualized dividend yield of the underlying asset.</value>
    public double DividendYield { get; init; }
}
