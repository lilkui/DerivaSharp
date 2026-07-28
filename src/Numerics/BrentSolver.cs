using CommunityToolkit.Diagnostics;

namespace DerivaSharp.Numerics;

/// <summary>
///     Brent-Dekker root-finding algorithm for continuous functions.
/// </summary>
public static class BrentSolver
{
    /// <summary>
    ///     Machine epsilon for positive <see cref="double" /> values.
    /// </summary>
    private const double PositiveDoublePrecision = 2.2204460492503131e-16;

    /// <summary>
    ///     Attempts to find a root of <c>f(x) = 0</c> in the interval [<paramref name="lowerBound" />,
    ///     <paramref name="upperBound" />].
    /// </summary>
    /// <param name="f">The function to find the root of; must return values of opposite sign at <paramref name="lowerBound" /> and <paramref name="upperBound" />.</param>
    /// <param name="lowerBound">The lower bound of the search interval.</param>
    /// <param name="upperBound">The upper bound of the search interval.</param>
    /// <param name="accuracy">The absolute tolerance for convergence.</param>
    /// <param name="maxIterations">The maximum number of iterations allowed.</param>
    /// <param name="root">When this method returns, contains the root if found; otherwise, the last computed value. This parameter is treated as uninitialized.</param>
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
        Guard.IsNotNull(f);
        Guard.IsLessThan(lowerBound, upperBound);
        Guard.IsGreaterThan(accuracy, 0d);
        Guard.IsGreaterThan(maxIterations, 0);

        root = lowerBound;
        double fPrevious = f(root);
        if (fPrevious == 0)
        {
            return true;
        }

        if (double.IsNaN(fPrevious))
        {
            return false;
        }

        root = upperBound;
        double fRoot = f(root);
        if (fRoot == 0)
        {
            return true;
        }

        if (double.IsNaN(fRoot) || HaveSameSign(fPrevious, fRoot))
        {
            return false;
        }

        double previous = lowerBound;
        double bracket = root;
        double fBracket = fRoot;
        double step = 0;
        double previousStep = 0;

        for (int iteration = 0; iteration < maxIterations; iteration++)
        {
            if (HaveSameSign(fRoot, fBracket))
            {
                bracket = previous;
                fBracket = fPrevious;
                previousStep = step = root - previous;
            }

            double absFRoot = Math.Abs(fRoot);
            double absFBracket = Math.Abs(fBracket);
            if (absFBracket < absFRoot)
            {
                previous = root;
                root = bracket;
                bracket = previous;
                fPrevious = fRoot;
                fRoot = fBracket;
                fBracket = fPrevious;
                absFRoot = absFBracket;
            }

            double tolerance = PositiveDoublePrecision * Math.Abs(root) + 0.5 * accuracy;
            double halfInterval = (bracket - root) * 0.5;

            if (Math.Abs(halfInterval) <= tolerance || absFRoot <= accuracy)
            {
                return true;
            }

            if (Math.Abs(previousStep) >= tolerance && Math.Abs(fPrevious) > absFRoot)
            {
                double s = fRoot / fPrevious;
                double p;
                double q;

                if (previous == bracket)
                {
                    p = 2 * halfInterval * s;
                    q = 1 - s;
                }
                else
                {
                    q = fPrevious / fBracket;
                    double r = fRoot / fBracket;
                    p = s * (2 * halfInterval * q * (q - r) - (root - previous) * (r - 1));
                    q = (q - 1) * (r - 1) * (s - 1);
                }

                if (p > 0)
                {
                    q = -q;
                }

                p = Math.Abs(p);
                if (2 * p < Math.Min(3 * halfInterval * q - Math.Abs(tolerance * q), Math.Abs(previousStep * q)))
                {
                    previousStep = step;
                    step = p / q;
                }
                else
                {
                    previousStep = step = halfInterval;
                }
            }
            else
            {
                previousStep = step = halfInterval;
            }

            previous = root;
            fPrevious = fRoot;
            root += Math.Abs(step) > tolerance ? step : Math.CopySign(tolerance, halfInterval);
            fRoot = f(root);
            if (fRoot == 0)
            {
                return true;
            }

            if (double.IsNaN(fRoot))
            {
                return false;
            }
        }

        return false;
    }

    private static bool HaveSameSign(double x, double y) => (x < 0) == (y < 0);
}
