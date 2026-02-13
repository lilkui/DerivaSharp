using DerivaSharp.Numerics;

namespace DerivaSharp.Tests;

public class BrentRootFinderTest
{
    [Fact]
    public void FindRoot_ReturnsExpectedValue_ForBracketedRoot()
    {
        double root = BrentRootFinder.FindRoot(x => x * x - 2, 0, 2, 1e-12);

        Assert.InRange(root, Math.Sqrt(2) - 1e-10, Math.Sqrt(2) + 1e-10);
    }

    [Fact]
    public void TryFindRoot_ReturnsFalse_WhenRootIsNotBracketed()
    {
        bool converged = BrentRootFinder.TryFindRoot(x => x * x + 1, -1, 1, 1e-10, 100, out _);

        Assert.False(converged);
    }

    [Fact]
    public void FindRoot_Throws_WhenRootIsNotBracketed() => Assert.Throws<NonConvergenceException>(() => BrentRootFinder.FindRoot(x => x * x + 1, -1, 1));

    [Fact]
    public void TryFindRoot_FindsEndpointRoot()
    {
        bool converged = BrentRootFinder.TryFindRoot(x => x - 1, 1, 2, 1e-10, 100, out double root);

        Assert.True(converged);
        Assert.Equal(1, root);
    }
}
