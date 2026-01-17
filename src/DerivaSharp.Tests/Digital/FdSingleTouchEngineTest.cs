using DerivaSharp.Instruments;
using DerivaSharp.Models;
using DerivaSharp.PricingEngines;

namespace DerivaSharp.Tests;

public class FdSingleTouchEngineTest
{
    private readonly FdSingleTouchEngine _fdEngine = new(FiniteDifferenceScheme.CrankNicolson, 1000, 1000);
    private readonly AnalyticBinaryBarrierEngine _analyticEngine = new();

    [Theory]
    [MemberData(nameof(SingleTouchOptionTestData.ValueData), MemberType = typeof(SingleTouchOptionTestData))]
    public void Value_IsAccurate(TouchType touchType, PaymentType paymentType, double barrier, double rebate, double assetPrice)
    {
        SingleTouchOption option = new(
            touchType,
            paymentType,
            SingleTouchOptionTestData.Strike,
            barrier,
            rebate,
            SingleTouchOptionTestData.EffectiveDate,
            SingleTouchOptionTestData.ExpirationDate);
        PricingContext<BsmModelParameters> ctx = new(SingleTouchOptionTestData.ModelParameters, assetPrice, SingleTouchOptionTestData.EffectiveDate);
        double actual = _fdEngine.Value(option, ctx);
        double expected = _analyticEngine.Value(option, ctx);
        double tolerance = Math.Abs(expected) * 0.0003;
        Assert.Equal(expected, actual, tolerance);
    }
}
