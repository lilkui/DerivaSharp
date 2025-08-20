using CommunityToolkit.Diagnostics;

namespace DerivaSharp.Numerics;

public sealed class LinearInterpolation
{
    /// <summary>
    ///     Performs linear interpolation on a given set of data points.
    /// </summary>
    /// <param name="xs">The x-coordinates of the data points. Must be sorted in ascending order.</param>
    /// <param name="ys">The y-coordinates of the data points.</param>
    /// <param name="xValue">The x-coordinate at which to interpolate.</param>
    /// <returns>The interpolated y-value.</returns>
    public static double InterpolateSorted(ReadOnlySpan<double> xs, ReadOnlySpan<double> ys, double xValue)
    {
        Guard.IsEqualTo(xs.Length, ys.Length);
        Guard.IsGreaterThan(xs.Length, 1);

        int index = xs.BinarySearch(xValue);

        if (index >= 0)
        {
            return ys[index];
        }

        index = ~index;

        if (index == 0)
        {
            index = 1;
        }

        if (index == xs.Length)
        {
            index = xs.Length - 1;
        }

        double x1 = xs[index - 1];
        double y1 = ys[index - 1];
        double x2 = xs[index];
        double y2 = ys[index];

        return y1 + (xValue - x1) * (y2 - y1) / (x2 - x1);
    }
}
