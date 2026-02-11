using MathNet.Numerics.Distributions;
using static System.Math;

namespace DerivaSharp.PricingEngines;

/// <summary>
///     Provides Black-Scholes-Merton option pricing calculations.
/// </summary>
internal static class BsmCalculator
{
    /// <summary>
    ///     Calculates the Black-Scholes-Merton option value.
    /// </summary>
    /// <param name="z">Option type: 1 for call, -1 for put.</param>
    /// <param name="spot">Current spot price of the underlying asset.</param>
    /// <param name="strike">Strike price of the option.</param>
    /// <param name="tau">Time to expiration in years.</param>
    /// <param name="vol">Volatility of the underlying asset.</param>
    /// <param name="r">Risk-free interest rate.</param>
    /// <param name="q">Dividend yield.</param>
    /// <returns>The option value.</returns>
    public static double CalculateValue(int z, double spot, double strike, double tau, double vol, double r, double q)
    {
        if (tau <= 0)
        {
            return Max(z * (spot - strike), 0);
        }

        if (vol == 0)
        {
            double forward = spot * Exp((r - q) * tau);
            return Max(z * (forward - strike), 0) * Exp(-r * tau);
        }

        double sqrtT = Sqrt(tau);
        double d1 = (Log(spot / strike) + (r - q + vol * vol / 2) * tau) / (vol * sqrtT);
        double d2 = d1 - vol * sqrtT;

        return z * (spot * Exp(-q * tau) * StdNormCdf(z * d1) - strike * Exp(-r * tau) * StdNormCdf(z * d2));
    }

    /// <summary>
    ///     Calculates the d1 and d2 parameters used in the Black-Scholes-Merton formula.
    /// </summary>
    /// <param name="spot">Current spot price of the underlying asset.</param>
    /// <param name="strike">Strike price of the option.</param>
    /// <param name="tau">Time to expiration in years.</param>
    /// <param name="vol">Volatility of the underlying asset.</param>
    /// <param name="r">Risk-free interest rate.</param>
    /// <param name="q">Dividend yield.</param>
    /// <returns>A tuple containing d1 and d2 values.</returns>
    public static (double D1, double D2) D1D2(double spot, double strike, double tau, double vol, double r, double q)
    {
        double sqrtT = Sqrt(tau);
        double d1 = (Log(spot / strike) + (r - q + vol * vol / 2) * tau) / (vol * sqrtT);
        double d2 = d1 - vol * sqrtT;

        return (d1, d2);
    }

    /// <summary>
    ///     Calculates the cumulative distribution function of the standard normal distribution.
    /// </summary>
    /// <param name="x">The value at which to evaluate the CDF.</param>
    /// <returns>The cumulative probability.</returns>
    public static double StdNormCdf(double x) => Normal.CDF(0, 1, x);

    /// <summary>
    ///     Calculates the probability density function of the standard normal distribution.
    /// </summary>
    /// <param name="x">The value at which to evaluate the PDF.</param>
    /// <returns>The probability density.</returns>
    public static double StdNormPdf(double x) => Normal.PDF(0, 1, x);
}
