namespace DerivaSharp.Numerics;

/// <summary>
///     Provides standard normal distribution functions.
/// </summary>
public static class StandardNormalDistribution
{
    private const double InvSqrt2 = 0.7071067811865475244008443621048490;
    private const double InvSqrt2Pi = 0.3989422804014326779399460599343819;

    /// <summary>
    ///     Calculates the cumulative distribution function (CDF) of the standard normal distribution.
    /// </summary>
    /// <param name="x">The value at which to evaluate the CDF.</param>
    /// <returns>The cumulative probability.</returns>
    public static double Cdf(double x) => 0.5 * SpecialFunctions.Erfc(-x * InvSqrt2);

    /// <summary>
    ///     Calculates the probability density function (PDF) of the standard normal distribution.
    /// </summary>
    /// <param name="x">The value at which to evaluate the PDF.</param>
    /// <returns>The probability density.</returns>
    public static double Pdf(double x) => InvSqrt2Pi * Math.Exp(-0.5 * x * x);
}
