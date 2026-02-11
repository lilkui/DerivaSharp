using CommunityToolkit.Diagnostics;
using CommunityToolkit.HighPerformance.Buffers;

namespace DerivaSharp.Numerics;

/// <summary>
///     Represents a tridiagonal matrix and provides operations for solving linear systems.
/// </summary>
/// <param name="lower">The lower diagonal elements.</param>
/// <param name="main">The main diagonal elements.</param>
/// <param name="upper">The upper diagonal elements.</param>
public sealed class TridiagonalMatrix(double[] lower, double[] main, double[] upper)
{
    /// <summary>
    ///     Gets the lower diagonal elements of the matrix.
    /// </summary>
    public double[] Lower { get; } = lower;

    /// <summary>
    ///     Gets the main diagonal elements of the matrix.
    /// </summary>
    public double[] Main { get; } = main;

    /// <summary>
    ///     Gets the upper diagonal elements of the matrix.
    /// </summary>
    public double[] Upper { get; } = upper;

    /// <summary>
    ///     Multiplies the tridiagonal matrix by a vector.
    /// </summary>
    /// <param name="vector">The vector to multiply.</param>
    /// <param name="result">The span to store the result.</param>
    public void Multiply(ReadOnlySpan<double> vector, Span<double> result)
    {
        int n = Main.Length;

        if (n == 0)
        {
            return;
        }

        if (Lower.Length != n || Upper.Length != n || vector.Length != n || result.Length != n)
        {
            ThrowHelper.ThrowArgumentException(ExceptionMessages.SpanLengthsMustMatch);
        }

        if (n > 1)
        {
            result[0] = Main[0] * vector[0] + Upper[0] * vector[1];
        }
        else
        {
            result[0] = Main[0] * vector[0];
        }

        for (int i = 1; i < n - 1; i++)
        {
            result[i] = Lower[i] * vector[i - 1] + Main[i] * vector[i] + Upper[i] * vector[i + 1];
        }

        if (n > 1)
        {
            result[n - 1] = Lower[n - 1] * vector[n - 2] + Main[n - 1] * vector[n - 1];
        }
    }

    /// <summary>
    ///     Solves the linear system Ax = b using the Thomas algorithm.
    /// </summary>
    /// <param name="rhs">The right-hand side vector b.</param>
    /// <param name="result">The span to store the solution vector x.</param>
    public void Solve(ReadOnlySpan<double> rhs, Span<double> result)
    {
        int n = Main.Length;

        if (Lower.Length != n || Upper.Length != n || rhs.Length != n || result.Length != n)
        {
            ThrowHelper.ThrowArgumentException(ExceptionMessages.SpanLengthsMustMatch);
        }

        using SpanOwner<double> upperPrimeBuffer = SpanOwner<double>.Allocate(n);
        using SpanOwner<double> rhsPrimeBuffer = SpanOwner<double>.Allocate(n);

        Span<double> upperPrime = upperPrimeBuffer.Span;
        Span<double> rhsPrime = rhsPrimeBuffer.Span;

        // Forward elimination
        upperPrime[0] = Upper[0] / Main[0];
        rhsPrime[0] = rhs[0] / Main[0];

        for (int i = 1; i < n - 1; i++)
        {
            double denominator = Main[i] - Lower[i] * upperPrime[i - 1];
            upperPrime[i] = Upper[i] / denominator;
            rhsPrime[i] = (rhs[i] - Lower[i] * rhsPrime[i - 1]) / denominator;
        }

        // Last row
        rhsPrime[n - 1] = (rhs[n - 1] - Lower[n - 1] * rhsPrime[n - 2]) / (Main[n - 1] - Lower[n - 1] * upperPrime[n - 2]);

        // Backward substitution
        result[n - 1] = rhsPrime[n - 1];

        for (int i = n - 2; i >= 0; i--)
        {
            result[i] = rhsPrime[i] - upperPrime[i] * result[i + 1];
        }
    }
}
