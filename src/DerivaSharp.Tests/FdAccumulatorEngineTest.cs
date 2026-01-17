using DerivaSharp.Instruments;
using DerivaSharp.Models;
using DerivaSharp.PricingEngines;
using TorchSharp;

namespace DerivaSharp.Tests;

public class FdAccumulatorEngineTest
{
    private static readonly bool s_useCuda = torch.cuda.is_available();
    private readonly BsmModelParameters _modelParameters = new(0.2, 0.02, 0.01);
    private readonly DateOnly _effectiveDate = new(2025, 1, 6);
    private readonly DateOnly _expirationDate = new(2025, 7, 6);

    [Theory]
    [InlineData(0.9)]
    [InlineData(1.0)]
    [InlineData(1.05)]
    public void Value_FdMatchesMc(double assetPrice)
    {
        Accumulator option = new(
            1.0,
            1.1,
            1.0,
            2.0,
            0.0,
            _effectiveDate,
            _expirationDate);

        PricingContext<BsmModelParameters> ctx = new(_modelParameters, assetPrice, _effectiveDate);

        FdAccumulatorEngine fdEngine = new(FiniteDifferenceScheme.CrankNicolson, 1000, 500);
        McAccumulatorEngine mcEngine = new(200000, s_useCuda);

        double fdValue = fdEngine.Value(option, ctx);
        double mcValue = mcEngine.Value(option, ctx);

        double tolerance = Math.Max(0.05, Math.Abs(mcValue) * 0.03);
        Assert.Equal(mcValue, fdValue, tolerance);
    }
}
