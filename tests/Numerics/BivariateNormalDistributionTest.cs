using DerivaSharp.Numerics;

namespace DerivaSharp.Tests;

[Trait("Category", "Numerics")]
public sealed class BivariateNormalDistributionTest
{
    [Theory]
    [InlineData(0.5, -0.25, 0.2)]
    [InlineData(0.5, -0.25, 0.5)]
    [InlineData(0.5, -0.25, 0.8)]
    [InlineData(0.5, -0.25, 0.95)]
    [InlineData(-1.2, 0.7, -0.8)]
    [InlineData(1.5, -0.75, -0.95)]
    public void Cdf_IsAccurate(double x, double y, double rho)
    {
        double expected = CalculateReferenceCdf(x, y, rho);

        double actual = BivariateNormalDistribution.Cdf(x, y, rho);

        Assert.Equal(expected, actual, 12);
    }

    [Fact]
    public void Cdf_AcrossRandomRange_IsAccurate()
    {
        Random random = new(42);

        for (int i = 0; i < 250; i++)
        {
            double x = (random.NextDouble() * 6) - 3;
            double y = (random.NextDouble() * 6) - 3;
            double rho = (random.NextDouble() * 1.8) - 0.9;
            double expected = CalculateReferenceCdf(x, y, rho);

            double actual = BivariateNormalDistribution.Cdf(x, y, rho);

            Assert.Equal(expected, actual, 11);
        }
    }

    [Theory]
    [InlineData(-0.99)]
    [InlineData(-0.8)]
    [InlineData(-0.2)]
    [InlineData(0)]
    [InlineData(0.2)]
    [InlineData(0.8)]
    [InlineData(0.99)]
    public void Cdf_AtOrigin_IsAccurate(double rho)
    {
        double expected = 0.25 + (Math.Asin(rho) / (2 * Math.PI));

        double actual = BivariateNormalDistribution.Cdf(0, 0, rho);

        Assert.Equal(expected, actual, 14);
    }

    [Theory]
    [InlineData(-1.2, 0.7, -0.95)]
    [InlineData(0.5, -0.25, -0.5)]
    [InlineData(1.5, 0.75, 0.8)]
    public void Cdf_IsSymmetric(double x, double y, double rho)
    {
        double expected = BivariateNormalDistribution.Cdf(y, x, rho);

        double actual = BivariateNormalDistribution.Cdf(x, y, rho);

        Assert.Equal(expected, actual, 14);
    }

    [Theory]
    [InlineData(-1.2, 0.7, -0.95)]
    [InlineData(0.5, -0.25, -0.5)]
    [InlineData(1.5, 0.75, 0.8)]
    public void Cdf_Complement_IsAccurate(double x, double y, double rho)
    {
        double expected = StandardNormalDistribution.Cdf(x);

        double actual = BivariateNormalDistribution.Cdf(x, y, rho) +
                        BivariateNormalDistribution.Cdf(x, -y, -rho);

        Assert.Equal(expected, actual, 14);
    }

    [Fact]
    public void Cdf_AtCorrelationLimits_IsAccurate()
    {
        const double x = 0.5;
        const double y = -0.25;

        double positiveCorrelation = BivariateNormalDistribution.Cdf(x, y, 1);
        double negativeCorrelation = BivariateNormalDistribution.Cdf(x, y, -1);
        double zeroCorrelation = BivariateNormalDistribution.Cdf(x, y, 0);

        Assert.Equal(StandardNormalDistribution.Cdf(Math.Min(x, y)), positiveCorrelation);
        Assert.Equal(
            Math.Max(StandardNormalDistribution.Cdf(x) - StandardNormalDistribution.Cdf(-y), 0),
            negativeCorrelation);
        Assert.Equal(
            StandardNormalDistribution.Cdf(x) * StandardNormalDistribution.Cdf(y),
            zeroCorrelation);
    }

    [Fact]
    public void Cdf_WithNonFiniteLimits_IsAccurate()
    {
        Assert.True(double.IsNaN(BivariateNormalDistribution.Cdf(double.NaN, 0, 0.5)));
        Assert.True(double.IsNaN(BivariateNormalDistribution.Cdf(0, double.NaN, 0.5)));
        Assert.Equal(0, BivariateNormalDistribution.Cdf(double.NegativeInfinity, 0, 0.5));
        Assert.Equal(0, BivariateNormalDistribution.Cdf(0, double.NegativeInfinity, 0.5));
        Assert.Equal(
            StandardNormalDistribution.Cdf(0.5),
            BivariateNormalDistribution.Cdf(double.PositiveInfinity, 0.5, 0.5));
        Assert.Equal(
            StandardNormalDistribution.Cdf(0.5),
            BivariateNormalDistribution.Cdf(0.5, double.PositiveInfinity, 0.5));
    }

    private static double CalculateReferenceCdf(double x, double y, double rho)
    {
        double conditionalStandardDeviation = Math.Sqrt((1 - rho) * (1 + rho));
        return GaussLegendreQuadrature.Integrate(
            value => StandardNormalDistribution.Pdf(value) *
                     StandardNormalDistribution.Cdf((y - (rho * value)) / conditionalStandardDeviation),
            -10,
            x,
            96);
    }
}
