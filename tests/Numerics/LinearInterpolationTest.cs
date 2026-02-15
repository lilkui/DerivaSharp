using DerivaSharp.Numerics;
using MathNet.Numerics.Interpolation;

namespace DerivaSharp.Tests;

public class LinearInterpolationTest
{
    [Fact]
    public void InterpolateSorted_MatchesMathNetLinearSpline_ForFixedQueries()
    {
        double[] xs = [0.0, 0.5, 1.5, 2.0, 4.0, 6.0];
        double[] ys = [2.0, 1.0, 2.5, 0.5, 3.0, 1.5];
        double[] queries = [0.0, 0.25, 0.5, 1.0, 1.5, 1.75, 3.0, 4.0, 5.5, 6.0];

        LinearSpline spline = LinearSpline.InterpolateSorted(xs, ys);

        foreach (double x in queries)
        {
            double expected = spline.Interpolate(x);
            double actual = LinearInterpolation.InterpolateSorted(x, xs, ys);

            double tolerance = 1e-12 * Math.Max(1.0, Math.Abs(expected));
            Assert.InRange(actual, expected - tolerance, expected + tolerance);
        }
    }

    [Fact]
    public void InterpolateSorted_MatchesMathNetLinearSpline_AcrossRandomData()
    {
        Random random = new(1234);

        for (int sample = 0; sample < 100; sample++)
        {
            int pointCount = random.Next(2, 40);
            double[] xs = new double[pointCount];
            double[] ys = new double[pointCount];

            double x = -5.0;
            for (int i = 0; i < pointCount; i++)
            {
                x += 0.05 + random.NextDouble();
                xs[i] = x;
                ys[i] = -10.0 + 20.0 * random.NextDouble();
            }

            LinearSpline spline = LinearSpline.InterpolateSorted(xs, ys);
            double lower = xs[0];
            double upper = xs[^1];

            for (int i = 0; i < 30; i++)
            {
                double query = lower + (upper - lower) * random.NextDouble();

                double expected = spline.Interpolate(query);
                double actual = LinearInterpolation.InterpolateSorted(query, xs, ys);

                double tolerance = 1e-12 * Math.Max(1.0, Math.Abs(expected));
                Assert.InRange(actual, expected - tolerance, expected + tolerance);
            }
        }
    }
}
