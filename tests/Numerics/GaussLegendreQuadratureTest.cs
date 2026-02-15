using System.Collections;
using System.Reflection;
using DerivaSharp.Numerics;
using MathNet.Numerics.Integration;

namespace DerivaSharp.Tests;

public class GaussLegendreQuadratureTest
{
    public enum TestFunction
    {
        Exp,
        Sin3x,
        InvOnePlusX2,
        LogShifted,
        CoshMinusX2,
        Gaussian,
        GaussianWeighted,
    }

    public static TheoryData<TestFunction, double, double, int> IntegrationCases =>
        new()
        {
            { TestFunction.Exp, -1.0, 1.0, 2 },
            { TestFunction.Sin3x, -2.0, 1.5, 7 },
            { TestFunction.InvOnePlusX2, -5.0, 5.0, 20 },
            { TestFunction.LogShifted, -3.0, 2.0, 32 },
            { TestFunction.CoshMinusX2, -1.25, 2.75, 96 },
            { TestFunction.Gaussian, -4.0, 3.0, 128 },
            { TestFunction.GaussianWeighted, -2.0, 2.0, 129 },
        };

    [Theory]
    [MemberData(nameof(IntegrationCases))]
    public void Integrate_MatchesMathNetReference_ForRepresentativeCases(TestFunction function, double intervalBegin, double intervalEnd, int order)
    {
        Func<double, double> f = GetFunction(function);

        double expected = GaussLegendreRule.Integrate(f, intervalBegin, intervalEnd, order);
        double actual = GaussLegendreQuadrature.Integrate(f, intervalBegin, intervalEnd, order);

        double tolerance = 1e-14 * Math.Max(1.0, Math.Abs(expected));
        Assert.InRange(actual, expected - tolerance, expected + tolerance);
    }

    [Fact]
    public void Integrate_MatchesMathNetReference_AcrossRandomIntervalsAndOrders()
    {
        Random random = new(42);
        Func<double, double> f = x => Math.Exp(-x * x) + 0.25 * x * x * x - 0.5 * x;
        int[] orders = [3, 8, 19, 64, 129];

        for (int i = 0; i < 200; i++)
        {
            double intervalBegin = -5.0 + 10.0 * random.NextDouble();
            double intervalEnd = -5.0 + 10.0 * random.NextDouble();
            int order = orders[i % orders.Length];

            double expected = GaussLegendreRule.Integrate(f, intervalBegin, intervalEnd, order);
            double actual = GaussLegendreQuadrature.Integrate(f, intervalBegin, intervalEnd, order);

            double tolerance = 1e-14 * Math.Max(1.0, Math.Abs(expected));
            Assert.InRange(actual, expected - tolerance, expected + tolerance);
        }
    }

    [Fact]
    public void LookupTable_Values_MatchExpectedFormula()
    {
        // Expected values: (n-1)/n for n >= 1, with index 0 and 1 being 0
        // The lookup table has indices 0 to 128
        double[] expected = new double[129];

        expected[0] = 0.0; // Not used (n=0 doesn't make sense)
        expected[1] = 0.0; // (1-1)/1 = 0

        for (int n = 2; n <= 128; n++)
        {
            expected[n] = (n - 1.0) / n; // = 1 - 1/n
        }

        // Access the private lookup table via reflection
        double[] lookupTable = GetPrivateLookupTable();

        Assert.NotNull(lookupTable);
        Assert.Equal(expected.Length, lookupTable.Length);

        for (int i = 0; i < expected.Length; i++)
        {
            double expectedValue = expected[i];
            double actualValue = lookupTable[i];
            const double tolerance = 1e-15;
            Assert.True(
                Math.Abs(expectedValue - actualValue) < tolerance,
                $"Lookup table mismatch at index {i}: expected {expectedValue}, got {actualValue}");
        }
    }

    [Fact]
    public void PrecomputedPoints_Values_MatchGenerationAlgorithm()
    {
        IEnumerable<KeyValuePair<int, object>> precomputedPoints = GetPrivatePrecomputedPoints();

        foreach ((int order, object point) in precomputedPoints)
        {
            // Generate points using the same algorithm
            (double[] generatedAbscissas, double[] generatedWeights) = GenerateGaussLegendrePoints(order, 1e-15);

            double[] storedAbscissas = GetPointAbscissas(point);
            double[] storedWeights = GetPointWeights(point);

            int m = (order + 1) / 2;
            Assert.Equal(m, storedAbscissas.Length);
            Assert.Equal(m, generatedAbscissas.Length);

            const double abscissaTolerance = 1e-15;
            const double weightTolerance = 1e-15;

            for (int i = 0; i < m; i++)
            {
                Assert.True(
                    Math.Abs(storedAbscissas[i] - generatedAbscissas[i]) < abscissaTolerance,
                    $"Order {order}, abscissa[{i}]: expected {generatedAbscissas[i]}, got {storedAbscissas[i]}, diff {Math.Abs(storedAbscissas[i] - generatedAbscissas[i])}");
                Assert.True(
                    Math.Abs(storedWeights[i] - generatedWeights[i]) < weightTolerance,
                    $"Order {order}, weight[{i}]: expected {generatedWeights[i]}, got {storedWeights[i]}, diff {Math.Abs(storedWeights[i] - generatedWeights[i])}");
            }
        }
    }

    [Fact]
    public void PrecomputedPoints_Weights_SumToTwo()
    {
        IEnumerable<KeyValuePair<int, object>> precomputedPoints = GetPrivatePrecomputedPoints();

        foreach ((int order, object point) in precomputedPoints)
        {
            double[] weights = GetPointWeights(point);

            // Compute integral of f(x) = 1 over [-1, 1], which should equal 2
            // Use the same logic as the Integrate method
            int m = (order + 1) / 2;
            double sum;

            if ((order & 1) != 0)
            {
                // Odd order: weights[0] is for x=0 (applied once), rest are applied twice
                sum = weights[0]; // f(0) * w[0], and f(0) = 1
                for (int i = 1; i < m; i++)
                {
                    sum += weights[i] * 2.0; // f(x) + f(-x) = 2
                }
            }
            else
            {
                // Even order: all weights are applied twice
                sum = 0.0;
                for (int i = 0; i < m; i++)
                {
                    sum += weights[i] * 2.0;
                }
            }

            const double tolerance = 1e-15;
            Assert.True(
                Math.Abs(sum - 2.0) < tolerance,
                $"Order {order}: integral of 1 over [-1,1] is {sum}, expected 2.0");
        }
    }

    [Fact]
    public void PrecomputedPoints_Abscissas_AreValidAndSorted()
    {
        IEnumerable<KeyValuePair<int, object>> precomputedPoints = GetPrivatePrecomputedPoints();

        foreach ((int order, object point) in precomputedPoints)
        {
            double[] abscissas = GetPointAbscissas(point);

            // For odd orders, the first abscissa should be 0 (the center point)
            int startIdx = 0;
            if ((order & 1) != 0)
            {
                Assert.Equal(0.0, abscissas[0], 15);
                startIdx = 1;
            }

            // Remaining abscissas should be in (0, 1) since we only store non-negative ones
            for (int i = startIdx; i < abscissas.Length; i++)
            {
                Assert.True(
                    abscissas[i] > 0 && abscissas[i] < 1,
                    $"Order {order}: abscissa[{i}] = {abscissas[i]} is not in (0, 1)");
            }

            // Abscissas should be sorted in ascending order
            for (int i = 1; i < abscissas.Length; i++)
            {
                Assert.True(
                    abscissas[i] > abscissas[i - 1],
                    $"Order {order}: abscissas not sorted at index {i}");
            }
        }
    }

    private static Func<double, double> GetFunction(TestFunction function) =>
        function switch
        {
            TestFunction.Exp => Math.Exp,
            TestFunction.Sin3x => x => Math.Sin(3.0 * x),
            TestFunction.InvOnePlusX2 => x => 1.0 / (1.0 + x * x),
            TestFunction.LogShifted => x => Math.Log(x + 3.5),
            TestFunction.CoshMinusX2 => x => Math.Cosh(x) - x * x,
            TestFunction.Gaussian => x => Math.Exp(-x * x),
            TestFunction.GaussianWeighted => x => Math.Exp(-x * x) * (1.0 + x * x),
            _ => throw new ArgumentOutOfRangeException(nameof(function), function, null),
        };

    private static double[] GetPrivateLookupTable()
    {
        FieldInfo? field = typeof(GaussLegendreQuadrature).GetField(
            "s_legendreLookupTable",
            BindingFlags.NonPublic | BindingFlags.Static);
        return (double[])field!.GetValue(null)!;
    }

    private static IEnumerable<KeyValuePair<int, object>> GetPrivatePrecomputedPoints()
    {
        FieldInfo? field = typeof(GaussLegendreQuadrature).GetField(
            "s_precomputedPoints",
            BindingFlags.NonPublic | BindingFlags.Static);
        IDictionary? dictionary = field!.GetValue(null) as IDictionary;

        foreach (DictionaryEntry entry in dictionary!)
        {
            yield return new KeyValuePair<int, object>((int)entry.Key, entry.Value!);
        }
    }

    private static double[] GetPointAbscissas(object point)
    {
        PropertyInfo? property = point.GetType().GetProperty("Abscissas");
        return (double[])property!.GetValue(point)!;
    }

    private static double[] GetPointWeights(object point)
    {
        PropertyInfo? property = point.GetType().GetProperty("Weights");
        return (double[])property!.GetValue(point)!;
    }

    private static (double[] Abscissas, double[] Weights) GenerateGaussLegendrePoints(int n, double epsilon)
    {
        int m = (n + 1) / 2;
        double[] abscissas = new double[m];
        double[] weights = new double[m];

        for (int i = 1; i <= m; i++)
        {
            // Initial guess using Chebyshev root approximation
            double z = Math.Cos(Math.PI * (i - 0.25) / (n + 0.5));

            double z1;
            do
            {
                // Compute Legendre polynomial P_n(z) and its derivative P'_n(z)
                double lp1 = 1.0;
                double lp2 = 0.0;

                for (int j = 1; j <= n; j++)
                {
                    double lp3 = lp2;
                    lp2 = lp1;
                    lp1 = ((2.0 * j - 1.0) * z * lp2 - (j - 1.0) * lp3) / j;
                }

                double lpp = n * (z * lp1 - lp2) / (z * z - 1.0);
                z1 = z;
                z -= lp1 / lpp;
            }
            while (Math.Abs(z - z1) > epsilon);

            int index = m - i;
            abscissas[index] = z;

            // Compute weight more accurately
            double p1 = 1.0, p2 = 0.0;
            for (int j = 1; j <= n; j++)
            {
                double p3 = p2;
                p2 = p1;
                p1 = ((2.0 * j - 1.0) * z * p2 - (j - 1.0) * p3) / j;
            }

            double pp = n * (z * p1 - p2) / (z * z - 1.0);
            weights[index] = 2.0 / ((1.0 - z * z) * pp * pp);
        }

        return (abscissas, weights);
    }
}
