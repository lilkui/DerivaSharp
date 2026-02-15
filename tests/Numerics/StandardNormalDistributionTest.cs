using MathNet.Numerics.Distributions;
using StandardNormalDistribution = DerivaSharp.Numerics.StandardNormalDistribution;

namespace DerivaSharp.Tests;

public class StandardNormalDistributionTest
{
    [Theory]
    [InlineData(-10)]
    [InlineData(-5)]
    [InlineData(-2)]
    [InlineData(-1)]
    [InlineData(-0.5)]
    [InlineData(0)]
    [InlineData(0.5)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(5)]
    [InlineData(10)]
    public void Cdf_MatchesMathNet_AtRepresentativePoints(double x)
    {
        double expected = Normal.CDF(0.0, 1.0, x);
        double actual = StandardNormalDistribution.Cdf(x);

        double tolerance = 1e-14 * Math.Max(1.0, Math.Abs(expected));
        Assert.InRange(actual, expected - tolerance, expected + tolerance);
    }

    [Theory]
    [InlineData(-10)]
    [InlineData(-5)]
    [InlineData(-2)]
    [InlineData(-1)]
    [InlineData(-0.5)]
    [InlineData(0)]
    [InlineData(0.5)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(5)]
    [InlineData(10)]
    public void Pdf_MatchesMathNet_AtRepresentativePoints(double x)
    {
        double expected = Normal.PDF(0.0, 1.0, x);
        double actual = StandardNormalDistribution.Pdf(x);

        double tolerance = 1e-14 * Math.Max(1.0, Math.Abs(expected));
        Assert.InRange(actual, expected - tolerance, expected + tolerance);
    }

    [Fact]
    public void CdfAndPdf_MatchMathNet_AcrossRandomRange()
    {
        Random random = new(1234);

        for (int i = 0; i < 5000; i++)
        {
            double x = random.NextDouble() * 20.0 - 10.0;

            double expectedCdf = Normal.CDF(0.0, 1.0, x);
            double actualCdf = StandardNormalDistribution.Cdf(x);
            double cdfTolerance = 1e-14 * Math.Max(1.0, Math.Abs(expectedCdf));
            Assert.InRange(actualCdf, expectedCdf - cdfTolerance, expectedCdf + cdfTolerance);

            double expectedPdf = Normal.PDF(0.0, 1.0, x);
            double actualPdf = StandardNormalDistribution.Pdf(x);
            double pdfTolerance = 1e-14 * Math.Max(1.0, Math.Abs(expectedPdf));
            Assert.InRange(actualPdf, expectedPdf - pdfTolerance, expectedPdf + pdfTolerance);
        }
    }

    [Fact]
    public void CdfAndPdf_HandleSpecialValues()
    {
        Assert.True(double.IsNaN(StandardNormalDistribution.Cdf(double.NaN)));
        Assert.Equal(0.0, StandardNormalDistribution.Cdf(double.NegativeInfinity));
        Assert.Equal(1.0, StandardNormalDistribution.Cdf(double.PositiveInfinity));

        Assert.True(double.IsNaN(StandardNormalDistribution.Pdf(double.NaN)));
        Assert.Equal(0.0, StandardNormalDistribution.Pdf(double.NegativeInfinity));
        Assert.Equal(0.0, StandardNormalDistribution.Pdf(double.PositiveInfinity));
    }
}
