using MathNet.Numerics;
using MathNetSpecialFunctions = MathNet.Numerics.SpecialFunctions;
using SpecialFunctions = DerivaSharp.Numerics.SpecialFunctions;

namespace DerivaSharp.Tests;

public class SpecialFunctionsTest
{
    [Theory]
    [InlineData(-30)]
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
    [InlineData(30)]
    public void Erfc_MatchesRuntimeReference_AtRepresentativePoints(double x)
    {
        double expected = MathNetSpecialFunctions.Erfc(x);
        double actual = SpecialFunctions.Erfc(x);

        Assert.True(actual.AlmostEqual(expected));
    }

    [Fact]
    public void Erfc_MatchesRuntimeReference_AcrossWideRange()
    {
        Random random = new();

        for (int i = 0; i < 2000; i++)
        {
            double x = random.NextDouble() * 60 - 30;
            double expected = MathNetSpecialFunctions.Erfc(x);
            double actual = SpecialFunctions.Erfc(x);

            Assert.True(actual.AlmostEqual(expected));
        }
    }

    [Fact]
    public void Erfc_HandlesSpecialValues()
    {
        Assert.True(double.IsNaN(SpecialFunctions.Erfc(double.NaN)));
        Assert.Equal(2, SpecialFunctions.Erfc(double.NegativeInfinity));
        Assert.Equal(0, SpecialFunctions.Erfc(double.PositiveInfinity));
    }
}
