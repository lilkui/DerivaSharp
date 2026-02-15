using System.Runtime.CompilerServices;

namespace DerivaSharp.Numerics;

/// <summary>
///     Brent-Dekker root-finding algorithm for continuous functions.
/// </summary>
public static class BrentRootFinder
{
    /// <summary>
    ///     Machine epsilon for positive <see cref="double" /> values.
    /// </summary>
    private const double PositiveDoublePrecision = 2.2204460492503131e-16;

    /// <summary>
    ///     Attempts to find a root of <c>f(x) = 0</c> in the interval [<paramref name="lowerBound" />,
    ///     <paramref name="upperBound" />].
    /// </summary>
    /// <param name="f">Function to find the root of. Must have opposite signs at the bounds.</param>
    /// <param name="lowerBound">Lower bound of the search interval.</param>
    /// <param name="upperBound">Upper bound of the search interval.</param>
    /// <param name="accuracy">Absolute tolerance for convergence.</param>
    /// <param name="maxIterations">Maximum number of iterations allowed.</param>
    /// <param name="root">When this method returns, contains the root if found; otherwise, the last computed value.</param>
    /// <returns><see langword="true" /> if a root is found within the specified accuracy; otherwise, <see langword="false" />.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="f" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentOutOfRangeException">
    ///     <paramref name="lowerBound" /> ≥ <paramref name="upperBound" />,
    ///     <paramref name="accuracy" /> ≤ 0, or <paramref name="maxIterations" /> ≤ 0.
    /// </exception>
    public static bool TryFindRoot(
        Func<double, double> f,
        double lowerBound,
        double upperBound,
        double accuracy,
        int maxIterations,
        out double root)
    {
        ArgumentNullException.ThrowIfNull(f);

        if (lowerBound >= upperBound)
        {
            throw new ArgumentOutOfRangeException(nameof(lowerBound), "Must be strictly less than upper bound.");
        }

        if (!(accuracy > 0))
        {
            throw new ArgumentOutOfRangeException(nameof(accuracy), "Must be greater than zero.");
        }

        if (maxIterations <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxIterations), "Must be greater than zero.");
        }

        double fmin = f(lowerBound);
        if (fmin == 0)
        {
            root = lowerBound;
            return true;
        }

        double fmax = f(upperBound);
        root = upperBound;
        if (fmax == 0)
        {
            return true;
        }

        if (double.IsNaN(fmin) || double.IsNaN(fmax) || Math.Sign(fmin) == Math.Sign(fmax))
        {
            return false;
        }

        double froot = fmax;
        double d = 0;
        double e = 0;
        double xMid = double.NaN;

        for (int i = 0; i <= maxIterations; i++)
        {
            if (Math.Sign(froot) == Math.Sign(fmax))
            {
                upperBound = lowerBound;
                fmax = fmin;
                e = d = root - lowerBound;
            }

            if (Math.Abs(fmax) < Math.Abs(froot))
            {
                lowerBound = root;
                root = upperBound;
                upperBound = lowerBound;
                fmin = froot;
                froot = fmax;
                fmax = fmin;
            }

            double xAcc = PositiveDoublePrecision * Math.Abs(root) + 0.5 * accuracy;
            double prevXMid = xMid;
            xMid = (upperBound - root) * 0.5;

            if (Math.Abs(xMid) <= xAcc || Math.Abs(froot) <= accuracy)
            {
                return true;
            }

            if (Math.Abs(xMid - prevXMid) < 1e-14)
            {
                return false;
            }

            if (Math.Abs(e) >= xAcc && Math.Abs(fmin) > Math.Abs(froot))
            {
                double s = froot / fmin;
                double p;
                double q;

                if (AlmostEqualRelative(lowerBound, upperBound))
                {
                    p = 2 * xMid * s;
                    q = 1 - s;
                }
                else
                {
                    q = fmin / fmax;
                    double r = froot / fmax;
                    p = s * (2 * xMid * q * (q - r) - (root - lowerBound) * (r - 1));
                    q = (q - 1) * (r - 1) * (s - 1);
                }

                if (p > 0)
                {
                    q = -q;
                }

                p = Math.Abs(p);
                if (2 * p < Math.Min(3 * xMid * q - Math.Abs(xAcc * q), Math.Abs(e * q)))
                {
                    e = d;
                    d = p / q;
                }
                else
                {
                    d = xMid;
                    e = d;
                }
            }
            else
            {
                d = xMid;
                e = d;
            }

            lowerBound = root;
            fmin = froot;
            root += Math.Abs(d) > xAcc ? d : CopySigned(xAcc, xMid);
            froot = f(root);
            if (double.IsNaN(froot))
            {
                return false;
            }
        }

        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool AlmostEqualRelative(double x, double y)
    {
        double scale = Math.Max(Math.Max(Math.Abs(x), Math.Abs(y)), 1);
        return Math.Abs(x - y) <= PositiveDoublePrecision * scale;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static double CopySigned(double magnitude, double sign)
    {
        return sign >= 0 ? Math.Abs(magnitude) : -Math.Abs(magnitude);
    }
}
