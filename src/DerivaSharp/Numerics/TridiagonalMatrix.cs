using CommunityToolkit.Diagnostics;
using CommunityToolkit.HighPerformance.Buffers;

namespace DerivaSharp.Numerics;

public sealed class TridiagonalMatrix(double[] lower, double[] main, double[] upper)
{
    public double[] Lower { get; } = lower;

    public double[] Main { get; } = main;

    public double[] Upper { get; } = upper;

    public void Multiply(ReadOnlySpan<double> vector, Span<double> result)
    {
        int n = Main.Length;

        if (n == 0)
        {
            return;
        }

        if (Lower.Length != n || Upper.Length != n || vector.Length != n || result.Length != n)
        {
            ThrowHelper.ThrowArgumentException("All spans must have the same length.");
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

    public void Solve(ReadOnlySpan<double> rhs, Span<double> result)
    {
        int n = Main.Length;

        if (Lower.Length != n || Upper.Length != n || rhs.Length != n || result.Length != n)
        {
            ThrowHelper.ThrowArgumentException("All spans must have the same length.");
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
