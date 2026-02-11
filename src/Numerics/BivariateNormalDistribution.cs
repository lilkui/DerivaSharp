using CommunityToolkit.Diagnostics;
using MathNet.Numerics.Distributions;

namespace DerivaSharp.Numerics;

/// <summary>
///     Provides methods for computing the bivariate normal distribution probabilities.
/// </summary>
public static class BivariateNormalDistribution
{
    // Gauss-Legendre quadrature points (abscissas) for numerical integration.
    // The array is organized with 3 columns representing different accuracy levels based on correlation strength:
    // Column 0: For |rho| < 0.3 (3 points)
    // Column 1: For 0.3 ≤ |rho| < 0.75 (6 points)
    // Column 2: For |rho| ≥ 0.75 (10 points)
    private static readonly double[,] s_xx = new[,]
    {
        { -0.932469514203152, -0.981560634246719, -0.993128599185095 },
        { -0.661209386466265, -0.904117256370475, -0.963971927277914 },
        { -0.238619186083197, -0.769902674194305, -0.912234428251326 },
        { 0, -0.587317954286617, -0.839116971822219 },
        { 0, -0.36783149899818, -0.746331906460151 },
        { 0, -0.125233408511469, -0.636053680726515 },
        { 0, 0, -0.510867001950827 },
        { 0, 0, -0.37370608871542 },
        { 0, 0, -0.227785851141645 },
        { 0, 0, -7.65265211334973E-02 },
    };

    // Corresponding weights for the Gauss-Legendre quadrature points.
    // The array structure matches s_xx with 3 columns for different correlation strength ranges.
    // These weights are used to calculate the weighted sum in the numerical integration.
    private static readonly double[,] s_w = new[,]
    {
        { 0.17132449237917, 4.71753363865118E-02, 1.76140071391521E-02 },
        { 0.360761573048138, 0.106939325995318, 4.06014298003869E-02 },
        { 0.46791393457269, 0.160078328543346, 6.26720483341091E-02 },
        { 0, 0.203167426723066, 8.32767415767048E-02 },
        { 0, 0.233492536538355, 0.10193011981724 },
        { 0, 0.249147045813403, 0.118194531961518 },
        { 0, 0, 0.131688638449177 },
        { 0, 0, 0.142096109318382 },
        { 0, 0, 0.149172986472604 },
        { 0, 0, 0.152753387130726 },
    };

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

        // Select the appropriate column (ng) in the quadrature arrays and
        // the number of points (lg) based on the absolute value of correlation
        int ng, lg;

        switch (Math.Abs(rho))
        {
            case < 0.3:
                ng = 0;
                lg = 3;
                break;
            case < 0.75:
                ng = 1;
                lg = 6;
                break;
            default:
                ng = 2;
                lg = 10;
                break;
        }

        double h = -x;
        double k = -y;
        double hk = h * k;
        double bvn = 0;

        if (Math.Abs(rho) < 0.925)
        {
            if (Math.Abs(rho) > 0)
            {
                double hs = (h * h + k * k) / 2;
                double asr = Math.Asin(rho);

                for (int i = 0; i < lg; i++)
                {
                    for (int iss = -1; iss <= 1; iss += 2)
                    {
                        double sn = Math.Sin(asr * (iss * s_xx[i, ng] + 1) / 2);
                        bvn += s_w[i, ng] * Math.Exp((sn * hk - hs) / (1 - sn * sn));
                    }
                }

                bvn *= asr / (4 * Math.PI);
            }

            bvn += StdNormCdf(-h) * StdNormCdf(-k);
        }
        else
        {
            if (rho < 0)
            {
                k = -k;
                hk = -hk;
            }

            if (Math.Abs(rho) < 1)
            {
                double ass = (1 - rho) * (1 + rho);
                double a = Math.Sqrt(ass);
                double bs = Math.Pow(h - k, 2);
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
                    bvn -= Math.Exp(-hk / 2) * Math.Sqrt(2 * Math.PI) * StdNormCdf(-b / a) * b * (1 - c * bs * (1 - d * bs / 5) / 3);
                }

                a /= 2;

                for (int i = 0; i < lg; i++)
                {
                    for (int iss = -1; iss <= 1; iss += 2)
                    {
                        double xs = Math.Pow(a * (iss * s_xx[i, ng] + 1), 2);
                        double rs = Math.Sqrt(1 - xs);
                        asr = -(bs / xs + hk) / 2;

                        if (asr > -100)
                        {
                            bvn += a * s_w[i, ng] * Math.Exp(asr) * (Math.Exp(-hk * (1 - rs) / (2 * (1 + rs))) / rs - (1 + c * xs * (1 + d * xs)));
                        }
                    }
                }

                bvn = -bvn / (2 * Math.PI);
            }

            if (rho > 0)
            {
                bvn += StdNormCdf(-Math.Max(h, k));
            }
            else
            {
                bvn = -bvn;
                if (k > h)
                {
                    bvn += StdNormCdf(k) - StdNormCdf(h);
                }
            }
        }

        return bvn;
    }

    private static double StdNormCdf(double x) => Normal.CDF(0, 1, x);
}
