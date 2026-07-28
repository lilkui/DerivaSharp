using System.Numerics;
using CommunityToolkit.Diagnostics;
using CommunityToolkit.HighPerformance.Buffers;

namespace DerivaSharp.Numerics;

/// <summary>
///     Represents a tridiagonal matrix and provides operations for solving linear systems.
/// </summary>
public sealed class TridiagonalMatrix
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="TridiagonalMatrix"/> class.
    /// </summary>
    /// <param name="lower">The lower diagonal elements.</param>
    /// <param name="main">The main diagonal elements.</param>
    /// <param name="upper">The upper diagonal elements.</param>
    /// <exception cref="ArgumentNullException">A diagonal is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">The diagonals do not have the same length.</exception>
    public TridiagonalMatrix(double[] lower, double[] main, double[] upper)
    {
        ArgumentNullException.ThrowIfNull(lower);
        ArgumentNullException.ThrowIfNull(main);
        ArgumentNullException.ThrowIfNull(upper);

        if (lower.Length != main.Length || upper.Length != main.Length)
        {
            ThrowHelper.ThrowArgumentException(ExceptionMessages.SpanLengthsMustMatch);
        }

        Lower = lower;
        Main = main;
        Upper = upper;
    }

    /// <summary>
    ///     Gets the lower diagonal elements of the matrix.
    /// </summary>
    /// <value>An array of lower diagonal elements.</value>
    public double[] Lower { get; }

    /// <summary>
    ///     Gets the main diagonal elements of the matrix.
    /// </summary>
    /// <value>An array of main diagonal elements.</value>
    public double[] Main { get; }

    /// <summary>
    ///     Gets the upper diagonal elements of the matrix.
    /// </summary>
    /// <value>An array of upper diagonal elements.</value>
    public double[] Upper { get; }

    /// <summary>
    ///     Multiplies the tridiagonal matrix by a vector.
    /// </summary>
    /// <param name="vector">The vector to multiply.</param>
    /// <param name="result">The span to store the result.</param>
    /// <exception cref="ArgumentException">The vector and result do not match the matrix size.</exception>
    public void Multiply(ReadOnlySpan<double> vector, Span<double> result)
    {
        int n = Main.Length;

        if (vector.Length != n || result.Length != n)
        {
            ThrowHelper.ThrowArgumentException(ExceptionMessages.SpanLengthsMustMatch);
        }

        if (n == 0)
        {
            return;
        }

        ReadOnlySpan<double> lower = Lower;
        ReadOnlySpan<double> main = Main;
        ReadOnlySpan<double> upper = Upper;

        if (n == 1)
        {
            result[0] = main[0] * vector[0];
            return;
        }

        result[0] = main[0] * vector[0] + upper[0] * vector[1];

        int i = 1;
        int interiorLength = n - 2;
        int simdWidth = Vector<double>.Count;
        if (Vector.IsHardwareAccelerated && interiorLength >= simdWidth)
        {
            int simdLength = interiorLength - interiorLength % simdWidth;
            for (; i < 1 + simdLength; i += simdWidth)
            {
                Vector<double> lowerVector = new(lower.Slice(i, simdWidth));
                Vector<double> mainVector = new(main.Slice(i, simdWidth));
                Vector<double> upperVector = new(upper.Slice(i, simdWidth));
                Vector<double> previousVector = new(vector.Slice(i - 1, simdWidth));
                Vector<double> currentVector = new(vector.Slice(i, simdWidth));
                Vector<double> nextVector = new(vector.Slice(i + 1, simdWidth));

                Vector<double> resultVector = Vector.FusedMultiplyAdd(lowerVector, previousVector, mainVector * currentVector);
                resultVector = Vector.FusedMultiplyAdd(upperVector, nextVector, resultVector);
                resultVector.CopyTo(result.Slice(i, simdWidth));
            }
        }

        for (; i < n - 1; i++)
        {
            result[i] = lower[i] * vector[i - 1] + main[i] * vector[i] + upper[i] * vector[i + 1];
        }

        result[n - 1] = lower[n - 1] * vector[n - 2] + main[n - 1] * vector[n - 1];
    }

    /// <summary>
    ///     Solves the linear system Ax = b using the Thomas algorithm.
    /// </summary>
    /// <param name="rhs">The right-hand side vector b.</param>
    /// <param name="result">The span to store the solution vector x.</param>
    /// <exception cref="ArgumentException">The right-hand side and result do not match the matrix size.</exception>
    public void Solve(ReadOnlySpan<double> rhs, Span<double> result)
    {
        int n = Main.Length;

        if (rhs.Length != n || result.Length != n)
        {
            ThrowHelper.ThrowArgumentException(ExceptionMessages.SpanLengthsMustMatch);
        }

        if (n == 0)
        {
            return;
        }

        if (n == 1)
        {
            result[0] = rhs[0] / Main[0];
            return;
        }

        using SpanOwner<double> upperPrimeBuffer = SpanOwner<double>.Allocate(n);
        using SpanOwner<double> rhsPrimeBuffer = SpanOwner<double>.Allocate(n);

        Span<double> upperPrime = upperPrimeBuffer.Span;
        Span<double> rhsPrime = rhsPrimeBuffer.Span;

        upperPrime[0] = Upper[0] / Main[0];
        rhsPrime[0] = rhs[0] / Main[0];

        for (int i = 1; i < n - 1; i++)
        {
            double denominator = Main[i] - Lower[i] * upperPrime[i - 1];
            upperPrime[i] = Upper[i] / denominator;
            rhsPrime[i] = (rhs[i] - Lower[i] * rhsPrime[i - 1]) / denominator;
        }

        int last = n - 1;
        rhsPrime[last] = (rhs[last] - Lower[last] * rhsPrime[last - 1]) / (Main[last] - Lower[last] * upperPrime[last - 1]);
        result[last] = rhsPrime[last];

        for (int i = n - 2; i >= 0; i--)
        {
            result[i] = rhsPrime[i] - upperPrime[i] * result[i + 1];
        }
    }
}
