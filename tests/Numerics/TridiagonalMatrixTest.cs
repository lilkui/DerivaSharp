using System.Numerics;
using DerivaSharp.Numerics;

namespace DerivaSharp.Tests;

[Trait("Category", "Numerics")]
public sealed class TridiagonalMatrixTest
{
    [Fact]
    public void Constructor_WithDifferentDiagonalLengths_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => new TridiagonalMatrix([0.0], [2.0, 2.0], [0.0, 0.0]));
    }

    [Fact]
    public void Multiply_WithEmptyMatrix_ReturnsEmptyResult()
    {
        TridiagonalMatrix matrix = new([], [], []);

        matrix.Multiply(ReadOnlySpan<double>.Empty, Span<double>.Empty);
    }

    [Fact]
    public void Multiply_WithSingleElement_ReturnsExpectedValue()
    {
        TridiagonalMatrix matrix = new([0.0], [2.0], [0.0]);
        double[] result = new double[1];

        matrix.Multiply([3.0], result);

        Assert.Equal(6.0, result[0]);
    }

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

    [Fact]
    public void Solve_WithEmptyMatrix_ReturnsEmptyResult()
    {
        TridiagonalMatrix matrix = new([], [], []);

        matrix.Solve(ReadOnlySpan<double>.Empty, Span<double>.Empty);
    }

    [Fact]
    public void Solve_WithSingleRow_ReturnsExpectedValue()
    {
        TridiagonalMatrix matrix = new([0.0], [2.0], [0.0]);
        double[] result = new double[1];

        matrix.Solve([6.0], result);

        Assert.Equal(3.0, result[0]);
    }

    [Fact]
    public void Solve_WithSeparateSpans_ReturnsExpectedValues()
    {
        TridiagonalMatrix matrix = CreateThreeRowMatrix();
        double[] result = new double[3];

        matrix.Solve([1.0, 0.0, 1.0], result);

        Assert.Equal(1.0, result[0], 12);
        Assert.Equal(1.0, result[1], 12);
        Assert.Equal(1.0, result[2], 12);
    }

    [Fact]
    public void Solve_InPlace_ReturnsExpectedValues()
    {
        TridiagonalMatrix matrix = CreateThreeRowMatrix();
        double[] rhsAndResult = [1.0, 0.0, 1.0];

        matrix.Solve(rhsAndResult, rhsAndResult);

        Assert.Equal(1.0, rhsAndResult[0], 12);
        Assert.Equal(1.0, rhsAndResult[1], 12);
        Assert.Equal(1.0, rhsAndResult[2], 12);
    }

    [Fact]
    public void Solve_WithPartiallyOverlappingSpans_ReturnsExpectedValues()
    {
        TridiagonalMatrix matrix = CreateThreeRowMatrix();
        double[] storage = [1.0, 0.0, 1.0, 0.0];

        matrix.Solve(storage.AsSpan(0, 3), storage.AsSpan(1, 3));

        Assert.Equal(1.0, storage[1], 12);
        Assert.Equal(1.0, storage[2], 12);
        Assert.Equal(1.0, storage[3], 12);
    }

    private static TridiagonalMatrix CreateThreeRowMatrix() => new(
        [0.0, -1.0, -1.0],
        [2.0, 2.0, 2.0],
        [-1.0, -1.0, 0.0]);
}
