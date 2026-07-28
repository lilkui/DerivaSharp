using DerivaSharp.Numerics;

namespace DerivaSharp.Tests;

[Trait("Category", "Numerics")]
public sealed class BrentSolverTest
{
    [Fact]
    public void TryFindRoot_ReturnsExpectedValue_ForBracketedRoot()
    {
        bool converged = BrentSolver.TryFindRoot(x => x * x - 2, 0, 2, 1e-12, 100, out double root);

        Assert.True(converged);
        Assert.InRange(root, Math.Sqrt(2) - 1e-10, Math.Sqrt(2) + 1e-10);
    }

    [Theory]
    [InlineData(1, 10)]
    [InlineData(-10, 2)]
    public void TryFindRoot_ReturnsExpectedValue_ForAsymmetricBracket(double lowerBound, double upperBound)
    {
        bool converged = BrentSolver.TryFindRoot(x => x * x * x - x - 2, lowerBound, upperBound, 1e-12, 100, out double root);

        Assert.True(converged);
        Assert.Equal(1.52137970680457, root, 10);
    }

    [Fact]
    public void TryFindRoot_ReturnsFalse_WhenRootIsNotBracketed()
    {
        bool converged = BrentSolver.TryFindRoot(x => x * x + 1, -1, 1, 1e-10, 100, out _);

        Assert.False(converged);
    }

    [Fact]
    public void TryFindRoot_FindsEndpointRoot()
    {
        bool converged = BrentSolver.TryFindRoot(x => x - 1, 1, 2, 1e-10, 100, out double root);

        Assert.True(converged);
        Assert.Equal(1, root);
    }

    [Fact]
    public void TryFindRoot_ReturnsFalse_WhenFirstValueIsNaN()
    {
        int evaluations = 0;

        bool converged = BrentSolver.TryFindRoot(Function, 0, 2, 1e-10, 100, out double root);

        Assert.False(converged);
        Assert.Equal(0, root);
        Assert.Equal(1, evaluations);

        double Function(double x)
        {
            evaluations++;
            return double.NaN;
        }
    }

    [Fact]
    public void TryFindRoot_StopsAfterMaximumIterations()
    {
        int evaluations = 0;

        bool converged = BrentSolver.TryFindRoot(Function, 0, 2, 1e-12, 1, out _);

        Assert.False(converged);
        Assert.Equal(3, evaluations);

        double Function(double x)
        {
            evaluations++;
            return x * x - 2;
        }
    }
}
