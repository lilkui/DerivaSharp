using DerivaSharp.Instruments;
using DerivaSharp.Models;
using DerivaSharp.PricingEngines;
using TorchSharp;

namespace DerivaSharp.Tests;

public class FdAccumulatorEngineTest
{
    private static readonly bool s_useCuda = torch.cuda.is_available();

    [Theory]
    [InlineData(0.9)]
    [InlineData(1.0)]
    [InlineData(1.05)]
    public void Value_FdMatchesMc(double assetPrice)
    {
        var effectiveDate = new DateOnly(2025, 1, 6);
        DateOnly expirationDate = effectiveDate.AddDays(182);
        Accumulator option = new(
            1,
            1.1,
            1,
            2,
            0,
            effectiveDate,
            expirationDate);
        var modelParameters = new BsmModelParameters(0.3, 0.04, 0.01);
        PricingContext<BsmModelParameters> ctx = new(modelParameters, assetPrice, effectiveDate);
        FdAccumulatorEngine fdEngine = new(FiniteDifferenceScheme.CrankNicolson, 1000, 1000);
        McAccumulatorEngine mcEngine = new(200000, s_useCuda);
        double fdValue = fdEngine.Value(option, ctx);
        double mcValue = mcEngine.Value(option, ctx);
        double tolerance = Math.Max(0.05, Math.Abs(mcValue) * 0.03);
        Assert.Equal(mcValue, fdValue, tolerance);
    }
}
