using DerivaSharp.Instruments;
using DerivaSharp.Models;
using DerivaSharp.PricingEngines;

namespace DerivaSharp.Tests;

public class FdSingleTouchEngineTest
{
    private readonly BsmModelParameters _modelParameters = new(0.3, 0.04, 0.01);
    private readonly FdSingleTouchEngine _fdEngine = new(FiniteDifferenceScheme.CrankNicolson, 1000, 1000);
    private readonly AnalyticBinaryBarrierEngine _analyticEngine = new();

    public static TheoryData<TouchType, PaymentType, double, double> ValueTestData => new()
    {
        // TouchType, PaymentType, Barrier, Rebate
        { TouchType.OneTouchUp, PaymentType.PayAtHit, 110, 10 },
        { TouchType.OneTouchUp, PaymentType.PayAtExpiry, 110, 10 },
        { TouchType.OneTouchDown, PaymentType.PayAtHit, 90, 10 },
        { TouchType.OneTouchDown, PaymentType.PayAtExpiry, 90, 10 },
        { TouchType.NoTouchUp, PaymentType.PayAtExpiry, 110, 10 },
        { TouchType.NoTouchDown, PaymentType.PayAtExpiry, 90, 10 },
    };

    [Theory]
    [MemberData(nameof(ValueTestData))]
    public void Value_IsAccurate(TouchType touchType, PaymentType paymentType, double barrier, double rebate)
    {
        const double strike = 100;
        DateOnly effectiveDate = new(2025, 1, 6);
        DateOnly expirationDate = effectiveDate.AddDays(365);

        SingleTouchOption option = new(
            touchType,
            paymentType,
            strike,
            barrier,
            rebate,
            effectiveDate,
            expirationDate);

        const double assetPrice = 100;
        PricingContext<BsmModelParameters> ctx = new(_modelParameters, assetPrice, effectiveDate);

        double actual = _fdEngine.Value(option, ctx);
        double expected = _analyticEngine.Value(option, ctx);
        double tolerance = Math.Abs(expected) * 0.0003;
        Assert.Equal(expected, actual, tolerance);
    }
}
