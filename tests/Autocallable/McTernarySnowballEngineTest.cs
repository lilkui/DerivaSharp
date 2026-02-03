using DerivaSharp.Instruments;
using DerivaSharp.Models;
using DerivaSharp.PricingEngines;
using TorchSharp;

namespace DerivaSharp.Tests;

public class McTernarySnowballEngineTest
{
    private const double DefaultTolerance = 0.001;
    private static readonly bool s_useCuda = torch.cuda.is_available();
    private readonly DateOnly _effectiveDate;
    private readonly DateOnly _expirationDate;
    private readonly PricingContext<BsmModelParameters> _ctx;
    private readonly McTernarySnowballEngine _engine;

    public McTernarySnowballEngineTest()
    {
        _effectiveDate = new DateOnly(2022, 1, 5);
        _expirationDate = new DateOnly(2023, 1, 5);
        BsmModelParameters parameters = new(0.16, 0.02, 0.04);
        _ctx = new PricingContext<BsmModelParameters>(parameters, 1.0, _effectiveDate);
        _engine = new McTernarySnowballEngine(100000, s_useCuda);
    }

    [Fact]
    public void TernarySnowballValue_KnockOutOnObservationDate_IsAccurate()
    {
        const double koRate = 0.067;
        const double maturityRate = 0.04;
        const double minimalRate = 0.01;
        TernarySnowballOption option = TernarySnowballOption.Create(
            koRate,
            maturityRate,
            minimalRate,
            1.0,
            0.8,
            1.03,
            BarrierTouchStatus.NoTouch,
            _effectiveDate,
            _expirationDate);
        const double expected = 0.02;
        double actual = _engine.Value(option, _ctx);
        Assert.Equal(expected, actual, DefaultTolerance);
    }
}
