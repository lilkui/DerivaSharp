using System.Numerics;
using DerivaSharp.Numerics;

namespace DerivaSharp.Tests;

public class TridiagonalMatrixTest
{
    [Fact]
    public void Multiply_WithSimdAndScalarRemainder_ReturnsExpectedValues()
    {
        int n = Vector<double>.Count + 3;
        double[] lower = new double[n];
        double[] main = new double[n];
        double[] upper = new double[n];
        double[] vector = new double[n];
        double[] expected = new double[n];
        double[] actual = new double[n];

        for (int i = 0; i < n; i++)
        {
            lower[i] = -0.7 + 0.15 * i;
            main[i] = 2.0 + 0.2 * i;
            upper[i] = 1.1 - 0.1 * i;
            vector[i] = -1.0 + 0.25 * i;
        }

        lower[0] = 1234.5;
        upper[n - 1] = -9876.5;

        TridiagonalMatrix matrix = new(lower, main, upper);

        matrix.Multiply(vector, actual);

        expected[0] = main[0] * vector[0] + upper[0] * vector[1];

        for (int i = 1; i < n - 1; i++)
        {
            expected[i] = lower[i] * vector[i - 1] + main[i] * vector[i] + upper[i] * vector[i + 1];
        }

        expected[n - 1] = lower[n - 1] * vector[n - 2] + main[n - 1] * vector[n - 1];

        for (int i = 0; i < n; i++)
        {
            Assert.Equal(expected[i], actual[i], 12);
        }
    }
}
