using DerivaSharp.Instruments;
using DerivaSharp.Models;
using DerivaSharp.PricingEngines;
using DerivaSharp.Time;
using TorchSharp;

namespace DerivaSharp.Tests;

public class McBinarySnowballEngineTest
{
    private const double DefaultTolerance = 0.001;
    private static readonly bool s_useCuda = torch.cuda.is_available();
    private readonly DateOnly _effectiveDate;
    private readonly DateOnly _expirationDate;
    private readonly PricingContext<BsmModelParameters> _ctx;
    private readonly McBinarySnowballEngine _engine;

    public McBinarySnowballEngineTest()
    {
        _effectiveDate = new DateOnly(2022, 1, 5);
        _expirationDate = new DateOnly(2023, 1, 5);
        BsmModelParameters parameters = new(0.16, 0.02, 0.04);
        _ctx = new PricingContext<BsmModelParameters>(parameters, 1.0, _effectiveDate, SseCalendar.Shared);
        _engine = new McBinarySnowballEngine(100000, s_useCuda);
    }

    [Fact]
    public void BinarySnowballValue_KnockOutOnObservationDate_IsAccurate()
    {
        const double koRate = 0.094;
        const double maturityRate = 0.01;
        BinarySnowballOption option = BinarySnowballOption.Create(
            koRate,
            maturityRate,
            1.0,
            1.03,
            BarrierTouchStatus.NoTouch,
            _effectiveDate,
            _expirationDate);
        const double expected = 0.02;
        double actual = _engine.Value(option, _ctx);
        Assert.Equal(expected, actual, DefaultTolerance);
    }

    [Fact]
    public void Value_UpTouch_ReturnsZero()
    {
        BinarySnowballOption option = BinarySnowballOption.Create(
                0.094,
                0.01,
                1.0,
                1.03,
                BarrierTouchStatus.NoTouch,
                _effectiveDate,
                _expirationDate) with
            {
                BarrierTouchStatus = BarrierTouchStatus.UpTouch,
            };

        double actual = _engine.Value(option, _ctx);

        Assert.Equal(0.0, actual);
    }

    [Fact]
    public void Values_UpTouch_ReturnsZeroArray()
    {
        BinarySnowballOption option = BinarySnowballOption.Create(
                0.094,
                0.01,
                1.0,
                1.03,
                BarrierTouchStatus.NoTouch,
                _effectiveDate,
                _expirationDate) with
            {
                BarrierTouchStatus = BarrierTouchStatus.UpTouch,
            };

        double[] assetPrices = [0.8, 1.0, 1.2];
        double[] actual = _engine.Values(option, _ctx, assetPrices);

        Assert.All(actual, value => Assert.Equal(0.0, value));
    }

    [Fact]
    public void Values_AtExpiry_MatchesTerminalPayoff()
    {
        BinarySnowballOption option = BinarySnowballOption.Create(
            0.094,
            0.01,
            1.0,
            1.03,
            BarrierTouchStatus.NoTouch,
            _effectiveDate,
            _expirationDate);

        PricingContext<BsmModelParameters> expiryContext = _ctx with { ValuationDate = _expirationDate };

        double[] assetPrices = [0.8, 1.03, 1.2];
        double[] expected = [0.01, 0.094, 0.094];
        double[] actual = _engine.Values(option, expiryContext, assetPrices);

        for (int i = 0; i < expected.Length; i++)
        {
            Assert.Equal(expected[i], actual[i], 12);
        }
    }
}
