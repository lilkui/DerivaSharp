using CommunityToolkit.Diagnostics;

namespace DerivaSharp.Numerics;

/// <summary>
///     Provides methods for linear interpolation of data points.
/// </summary>
public static class LinearInterpolation
{
    /// <summary>
    ///     Performs linear interpolation on a given set of data points where the x-coordinates are monotonically increasing.
    /// </summary>
    /// <param name="x">The x-coordinate at which to evaluate the interpolated value.</param>
    /// <param name="xs">The x-coordinates of the data points. Must be sorted in ascending order.</param>
    /// <param name="ys">The y-coordinates of the data points.</param>
    /// <param name="left">Value to return for <c>x &lt; xs[0]</c>. Default is <c>ys[0]</c>.</param>
    /// <param name="right">Value to return for <c>x &gt; xs[^1]</c>. Default is <c>ys[^1]</c>.</param>
    /// <returns>The interpolated y-value.</returns>
    public static double InterpolateSorted(double x, ReadOnlySpan<double> xs, ReadOnlySpan<double> ys, double? left = null, double? right = null)
    {
        Guard.IsEqualTo(xs.Length, ys.Length);
        Guard.IsNotEmpty(xs);

        if (xs.Length == 1)
        {
            return ys[0];
        }

        int index = xs.BinarySearch(x);

        if (index >= 0)
        {
            return ys[index];
        }

        index = ~index;

        if (index == 0)
        {
            return left ?? ys[0];
        }

        if (index == xs.Length)
        {
            return right ?? ys[^1];
        }

        double x1 = xs[index - 1];
        double y1 = ys[index - 1];
        double x2 = xs[index];
        double y2 = ys[index];

        return y1 + (x - x1) * (y2 - y1) / (x2 - x1);
    }
}
