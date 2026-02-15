using CommunityToolkit.Diagnostics;

namespace DerivaSharp.Numerics;

/// <summary>
///     Provides methods for computing the bivariate normal distribution probabilities.
/// </summary>
public static class BivariateNormalDistribution
{
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

        const double correlationTolerance = 1e-15;

        if (Math.Abs(rho) <= correlationTolerance)
        {
            return StandardNormalDistribution.Cdf(x) * StandardNormalDistribution.Cdf(y);
        }

        if (rho >= 1 - correlationTolerance)
        {
            return StandardNormalDistribution.Cdf(Math.Min(x, y));
        }

        if (rho <= -1 + correlationTolerance)
        {
            return Math.Max(StandardNormalDistribution.Cdf(x) - StandardNormalDistribution.Cdf(-y), 0d);
        }

        double absRho = Math.Abs(rho);
        int quadratureOrder = absRho switch
        {
            < 0.3 => 6,
            < 0.75 => 12,
            _ => 20,
        };

        double h = -x;
        double k = -y;
        double hk = h * k;
        double bvn = 0;

        if (absRho < 0.925)
        {
            if (absRho > 0)
            {
                double hs = (h * h + k * k) / 2;
                double asr = Math.Asin(rho);
                double integral = GaussLegendreQuadrature.Integrate(
                    t =>
                    {
                        double sn = Math.Sin(asr * t);
                        return Math.Exp((sn * hk - hs) / (1 - sn * sn));
                    },
                    0,
                    1,
                    quadratureOrder);

                bvn += asr * integral / (2 * Math.PI);
            }

            bvn += StandardNormalDistribution.Cdf(-h) * StandardNormalDistribution.Cdf(-k);
        }
        else
        {
            if (rho < 0)
            {
                k = -k;
                hk = -hk;
            }

            if (absRho < 1)
            {
                double ass = (1 - rho) * (1 + rho);
                double a = Math.Sqrt(ass);
                double bs = (h - k) * (h - k);
                double c = (4 - hk) / 8;
                double d = (12 - hk) / 16;
                double asr = -(bs / ass + hk) / 2;

                if (asr > -100)
                {
                    bvn = a * Math.Exp(asr) * (1 - c * (bs - ass) * (1 - d * bs / 5) / 3 + c * d * ass * ass / 5);
                }

                if (-hk < 100)
                {
                    double b = Math.Sqrt(bs);
                    bvn -= Math.Exp(-hk / 2) * Math.Sqrt(2 * Math.PI) * StandardNormalDistribution.Cdf(-b / a) * b * (1 - c * bs * (1 - d * bs / 5) / 3);
                }

                a /= 2;

                bvn += GaussLegendreQuadrature.Integrate(
                    z =>
                    {
                        double zs = a * (z + 1);
                        double xs = zs * zs;
                        double rs = Math.Sqrt(1 - xs);
                        double innerAsr = -(bs / xs + hk) / 2;

                        if (innerAsr <= -100)
                        {
                            return 0;
                        }

                        return a * Math.Exp(innerAsr) * (Math.Exp(-hk * (1 - rs) / (2 * (1 + rs))) / rs - (1 + c * xs * (1 + d * xs)));
                    },
                    -1,
                    1,
                    quadratureOrder);

                bvn = -bvn / (2 * Math.PI);
            }

            if (rho > 0)
            {
                bvn += StandardNormalDistribution.Cdf(-Math.Max(h, k));
            }
            else
            {
                bvn = -bvn;
                if (k > h)
                {
                    bvn += StandardNormalDistribution.Cdf(k) - StandardNormalDistribution.Cdf(h);
                }
            }
        }

        return bvn;
    }
}
