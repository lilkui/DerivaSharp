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

        double tolerance = 1e-12 * Math.Max(1.0, Math.Abs(expected));
        Assert.InRange(actual, expected - tolerance, expected + tolerance);
    }

    [Fact]
    public void Integrate_MatchesMathNetReference_AcrossRandomIntervalsAndOrders()
    {
        Random random = new(1234);
        Func<double, double> f = x => Math.Exp(-x * x) + 0.25 * x * x * x - 0.5 * x;
        int[] orders = [3, 8, 19, 64, 129];

        for (int i = 0; i < 200; i++)
        {
            double intervalBegin = -5.0 + 10.0 * random.NextDouble();
            double intervalEnd = -5.0 + 10.0 * random.NextDouble();
            int order = orders[i % orders.Length];

            double expected = GaussLegendreRule.Integrate(f, intervalBegin, intervalEnd, order);
            double actual = GaussLegendreQuadrature.Integrate(f, intervalBegin, intervalEnd, order);

            double tolerance = 1e-11 * Math.Max(1.0, Math.Abs(expected));
            Assert.InRange(actual, expected - tolerance, expected + tolerance);
        }
    }

    private static Func<double, double> GetFunction(TestFunction function) =>
        function switch
        {
            TestFunction.Exp => x => Math.Exp(x),
            TestFunction.Sin3x => x => Math.Sin(3.0 * x),
            TestFunction.InvOnePlusX2 => x => 1.0 / (1.0 + x * x),
            TestFunction.LogShifted => x => Math.Log(x + 3.5),
            TestFunction.CoshMinusX2 => x => Math.Cosh(x) - x * x,
            TestFunction.Gaussian => x => Math.Exp(-x * x),
            TestFunction.GaussianWeighted => x => Math.Exp(-x * x) * (1.0 + x * x),
            _ => throw new ArgumentOutOfRangeException(nameof(function), function, null),
        };
}
