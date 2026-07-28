using CommunityToolkit.Diagnostics;

namespace DerivaSharp.Numerics;

/// <summary>
///     Provides methods for computing the bivariate normal distribution probabilities.
/// </summary>
public static class BivariateNormalDistribution
{
    private const double CorrelationTolerance = 1e-15;
    private const double HighCorrelationThreshold = 0.925;
    private const double InvFourPi = 0.0795774715459476678844418816862572;
    private const double InvTwoPi = 0.1591549430918953357688837633725144;
    private const double SqrtTwoPi = 2.5066282746310005024157652848110453;

    private static ReadOnlySpan<double> Abscissas =>
    [
        0.2386191860831969086305017,
        0.6612093864662645136613996,
        0.9324695142031520278123016,
        0.1252334085114689154724414,
        0.3678314989981801937526915,
        0.5873179542866174472967024,
        0.7699026741943046870368938,
        0.9041172563704748566784659,
        0.9815606342467192506905491,
        0.0765265211334973337546404,
        0.2277858511416450780804962,
        0.3737060887154195606725482,
        0.5108670019508270980043641,
        0.6360536807265150254528367,
        0.7463319064601507926143051,
        0.8391169718222188233945291,
        0.9122344282513259058677524,
        0.9639719272779137912676661,
        0.9931285991850949247861224,
    ];

    private static ReadOnlySpan<double> Weights =>
    [
        0.4679139345726910473898703,
        0.3607615730481386075698335,
        0.1713244923791703450402961,
        0.2491470458134027850005624,
        0.2334925365383548087608499,
        0.2031674267230659217490645,
        0.1600783285433462263346525,
        0.1069393259953184309602547,
        0.0471753363865118271946160,
        0.1527533871307258506980843,
        0.1491729864726037467878287,
        0.1420961093183820513292983,
        0.1316886384491766268984945,
        0.1181945319615184173123774,
        0.1019301198172404350367501,
        0.0832767415767047487247581,
        0.0626720483341090635695065,
        0.0406014298003869413310400,
        0.0176140071391521183118620,
    ];

    /// <summary>
    ///     Computes the cumulative distribution function (CDF) of the standard bivariate normal distribution.
    /// </summary>
    /// <param name="x">The first upper integration limit.</param>
    /// <param name="y">The second upper integration limit.</param>
    /// <param name="rho">The correlation coefficient between X and Y, must be between -1 and 1.</param>
    /// <returns>
    ///     The probability P(X ≤ x, Y ≤ y) where (X,Y) follows a standard bivariate normal
    ///     distribution with correlation coefficient rho.
    /// </returns>
    public static double Cdf(double x, double y, double rho)
    {
        Guard.IsBetweenOrEqualTo(rho, -1, 1);

        if (double.IsNaN(x) || double.IsNaN(y))
        {
            return double.NaN;
        }

        if (double.IsNegativeInfinity(x) || double.IsNegativeInfinity(y))
        {
            return 0;
        }

        if (double.IsPositiveInfinity(x))
        {
            return StandardNormalDistribution.Cdf(y);
        }

        if (double.IsPositiveInfinity(y))
        {
            return StandardNormalDistribution.Cdf(x);
        }

        double absRho = Math.Abs(rho);
        if (absRho <= CorrelationTolerance)
        {
            return StandardNormalDistribution.Cdf(x) * StandardNormalDistribution.Cdf(y);
        }

        if (rho >= 1 - CorrelationTolerance)
        {
            return StandardNormalDistribution.Cdf(Math.Min(x, y));
        }

        if (rho <= -1 + CorrelationTolerance)
        {
            return Math.Max(StandardNormalDistribution.Cdf(x) - StandardNormalDistribution.Cdf(-y), 0);
        }

        int offset;
        int count;
        if (absRho < 0.3)
        {
            offset = 0;
            count = 3;
        }
        else if (absRho < 0.75)
        {
            offset = 3;
            count = 6;
        }
        else
        {
            offset = 9;
            count = 10;
        }

        ReadOnlySpan<double> abscissas = Abscissas.Slice(offset, count);
        ReadOnlySpan<double> weights = Weights.Slice(offset, count);
        double h = -x;
        double k = -y;
        double hk = h * k;

        if (absRho < HighCorrelationThreshold)
        {
            double halfSumOfSquares = 0.5 * ((h * h) + (k * k));
            double asinRho = Math.Asin(rho);
            double integral = IntegrateModerateCorrelation(abscissas, weights, asinRho, hk, halfSumOfSquares);

            return (asinRho * integral * InvFourPi) +
                   (StandardNormalDistribution.Cdf(x) * StandardNormalDistribution.Cdf(y));
        }

        if (rho < 0)
        {
            k = -k;
            hk = -hk;
        }

        double oneMinusRhoSquared = (1 - rho) * (1 + rho);
        double a = Math.Sqrt(oneMinusRhoSquared);
        double difference = h - k;
        double differenceSquared = difference * difference;
        double c = (4 - hk) / 8;
        double d = (12 - hk) / 16;
        double exponent = -0.5 * ((differenceSquared / oneMinusRhoSquared) + hk);
        double result = 0;

        if (exponent > -100)
        {
            result = a * Math.Exp(exponent) *
                     (1 - (c * (differenceSquared - oneMinusRhoSquared) *
                         (1 - (d * differenceSquared / 5)) / 3) +
                      (c * d * oneMinusRhoSquared * oneMinusRhoSquared / 5));
        }

        if (-hk < 100)
        {
            double absoluteDifference = Math.Abs(difference);
            result -= Math.Exp(-0.5 * hk) * SqrtTwoPi *
                      StandardNormalDistribution.Cdf(-absoluteDifference / a) * absoluteDifference *
                      (1 - (c * differenceSquared * (1 - (d * differenceSquared / 5)) / 3));
        }

        result += IntegrateHighCorrelation(abscissas, weights, 0.5 * a, differenceSquared, hk, c, d);
        result *= -InvTwoPi;

        if (rho > 0)
        {
            return result + StandardNormalDistribution.Cdf(-Math.Max(h, k));
        }

        result = -result;
        return k > h
            ? result + StandardNormalDistribution.Cdf(k) - StandardNormalDistribution.Cdf(h)
            : result;
    }

    private static double IntegrateModerateCorrelation(
        ReadOnlySpan<double> abscissas,
        ReadOnlySpan<double> weights,
        double asinRho,
        double hk,
        double halfSumOfSquares)
    {
        double sum = 0;
        for (int i = 0; i < abscissas.Length; i++)
        {
            double halfAbscissa = 0.5 * abscissas[i];
            double sin = Math.Sin(asinRho * (0.5 - halfAbscissa));
            double lowerValue = Math.Exp(((sin * hk) - halfSumOfSquares) / (1 - (sin * sin)));

            sin = Math.Sin(asinRho * (0.5 + halfAbscissa));
            double upperValue = Math.Exp(((sin * hk) - halfSumOfSquares) / (1 - (sin * sin)));

            sum += weights[i] * (lowerValue + upperValue);
        }

        return sum;
    }

    private static double IntegrateHighCorrelation(
        ReadOnlySpan<double> abscissas,
        ReadOnlySpan<double> weights,
        double halfA,
        double differenceSquared,
        double hk,
        double c,
        double d)
    {
        double sum = 0;
        for (int i = 0; i < abscissas.Length; i++)
        {
            double weightedSum = 0;
            for (int sign = -1; sign <= 1; sign += 2)
            {
                double z = halfA * ((sign * abscissas[i]) + 1);
                double zSquared = z * z;
                double root = Math.Sqrt(1 - zSquared);
                double exponent = -0.5 * ((differenceSquared / zSquared) + hk);

                if (exponent <= -100)
                {
                    continue;
                }

                double onePlusRoot = 1 + root;
                double correctionExponent = -hk * zSquared / (2 * onePlusRoot * onePlusRoot);
                weightedSum += halfA * Math.Exp(exponent) *
                               ((Math.Exp(correctionExponent) / root) -
                                (1 + (c * zSquared * (1 + (d * zSquared)))));
            }

            sum += weights[i] * weightedSum;
        }

        return sum;
    }
}
