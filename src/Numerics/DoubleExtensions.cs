namespace DerivaSharp.Numerics;

/// <summary>
///     Extension methods for <see cref="double" /> values.
/// </summary>
public static class DoubleExtensions
{
    extension(double x)
    {
        /// <summary>
        ///     Determines whether two double values are approximately equal within specified tolerances.
        /// </summary>
        /// <param name="y">The value to compare with.</param>
        /// <param name="absoluteTolerance">The maximum absolute difference allowed.</param>
        /// <param name="relativeTolerance">The maximum relative difference allowed as a fraction of the larger magnitude.</param>
        /// <returns><see langword="true" /> if the values are approximately equal; otherwise, <see langword="false" />.</returns>
        public bool AlmostEquals(double y, double absoluteTolerance = 1e-15, double relativeTolerance = 1e-12)
        {
            if (x == y)
            {
                return true;
            }

            if (double.IsNaN(x) || double.IsNaN(y))
            {
                return false;
            }

            if (double.IsInfinity(x) || double.IsInfinity(y))
            {
                return false;
            }

            double diff = Math.Abs(x - y);
            if (diff <= absoluteTolerance)
            {
                return true;
            }

            double scale = Math.Max(Math.Abs(x), Math.Abs(y));
            return diff <= scale * relativeTolerance;
        }
    }
}
