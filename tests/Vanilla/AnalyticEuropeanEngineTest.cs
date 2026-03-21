using DerivaSharp.Instruments;
using DerivaSharp.Models;
using DerivaSharp.PricingEngines;
using DerivaSharp.Time;
using static DerivaSharp.Tests.EuropeanOptionTestData;

namespace DerivaSharp.Tests;

public class AnalyticEuropeanEngineTest
{
    private readonly EuropeanOption _call;
    private readonly EuropeanOption _put;
    private readonly AnalyticEuropeanEngine _engine;

    public AnalyticEuropeanEngineTest()
    {
        DateOnly effectiveDate = EffectiveDate;
        DateOnly expirationDate = ExpirationDate;
        _call = new EuropeanOption(OptionType.Call, Strike, effectiveDate, expirationDate);
        _put = new EuropeanOption(OptionType.Put, Strike, effectiveDate, expirationDate);
        _engine = new AnalyticEuropeanEngine();
    }

    [Theory]
    [MemberData(nameof(ValueData), MemberType = typeof(EuropeanOptionTestData))]
    public void Value_IsAccurate(OptionType optionType, double assetPrice, double expected)
    {
        const int precision = 6;
        EuropeanOption option = optionType == OptionType.Call ? _call : _put;
        PricingContext<BsmModelParameters> ctx = new(ModelParameters, assetPrice, EffectiveDate, NullCalendar.Shared);
        double actual = _engine.Value(option, ctx);
        Assert.Equal(expected, actual, precision);
    }

    [Theory]
    [MemberData(nameof(GreekData), MemberType = typeof(EuropeanOptionTestData))]
    public void Greek_IsAccurate(string greek, OptionType optionType, double assetPrice, double expected)
    {
        const int precision = 6;
        EuropeanOption option = optionType == OptionType.Call ? _call : _put;
        PricingContext<BsmModelParameters> ctx = new(ModelParameters, assetPrice, EffectiveDate, NullCalendar.Shared);
        double actual = greek switch
        {
            "Delta" => _engine.Delta(option, ctx),
            "Gamma" => _engine.Gamma(option, ctx),
            "Speed" => _engine.Speed(option, ctx),
            "Theta" => _engine.Theta(option, ctx),
            "Vega" => _engine.Vega(option, ctx),
            "Vanna" => _engine.Vanna(option, ctx),
            "Zomma" => _engine.Zomma(option, ctx),
            "Rho" => _engine.Rho(option, ctx),
            "Charm" => _engine.Charm(option, ctx),
            "Color" => _engine.Color(option, ctx),
            _ => throw new ArgumentException("Invalid Greek"),
        };
        Assert.Equal(expected, actual, precision);
    }

    [Theory]
    [MemberData(nameof(ImpliedVolatilityData), MemberType = typeof(EuropeanOptionTestData))]
    public void ImpliedVolatility_IsAccurate(double price, double assetPrice, double expected)
    {
        const int precision = 4;
        PricingContext<BsmModelParameters> ctx = new(ModelParameters, assetPrice, EffectiveDate, NullCalendar.Shared);
        double actual = _engine.ImpliedVolatility(_call, ctx, price);
        Assert.Equal(expected, actual, precision);
    }

    [Theory]
    [MemberData(nameof(ValueAtExpiryData), MemberType = typeof(EuropeanOptionTestData))]
    public void Value_AtExpiry_ReturnsIntrinsicValue(OptionType optionType, double assetPrice, double expected)
    {
        const int precision = 6;
        EuropeanOption option = new(optionType, Strike, EffectiveDate, ExpirationDate);
        PricingContext<BsmModelParameters> ctx = new(ModelParameters, assetPrice, ExpirationDate, NullCalendar.Shared);
        double actual = _engine.Value(option, ctx);
        Assert.Equal(expected, actual, precision);
    }

    [Theory]
    [InlineData(OptionType.Call, 100)]
    [InlineData(OptionType.Put, 100)]
    public void NumericalAndAnalyticalGreeks_AreEqual(OptionType optionType, double assetPrice)
    {
        const int precision = 6;
        EuropeanOption option = optionType == OptionType.Call ? _call : _put;
        PricingContext<BsmModelParameters> ctx = new(ModelParameters, assetPrice, EffectiveDate, NullCalendar.Shared);

        // Numerical Greeks
        AnalyticEuropeanEngine numericalEngine = new() { UseNumericalGreeks = true };
        double deltaN = numericalEngine.Delta(option, ctx);
        double gammaN = numericalEngine.Gamma(option, ctx);
        double speedN = numericalEngine.Speed(option, ctx);
        double thetaN = numericalEngine.Theta(option, ctx);
        double charmN = numericalEngine.Charm(option, ctx);
        double colorN = numericalEngine.Color(option, ctx);
        double vegaN = numericalEngine.Vega(option, ctx);
        double vannaN = numericalEngine.Vanna(option, ctx);
        double zommaN = numericalEngine.Zomma(option, ctx);
        double rhoN = numericalEngine.Rho(option, ctx);

        // Analytical Greeks
        double deltaA = _engine.Delta(option, ctx);
        double gammaA = _engine.Gamma(option, ctx);
        double speedA = _engine.Speed(option, ctx);
        double thetaA = _engine.Theta(option, ctx);
        double charmA = _engine.Charm(option, ctx);
        double colorA = _engine.Color(option, ctx);
        double vegaA = _engine.Vega(option, ctx);
        double vannaA = _engine.Vanna(option, ctx);
        double zommaA = _engine.Zomma(option, ctx);
        double rhoA = _engine.Rho(option, ctx);

        Assert.Equal(deltaA, deltaN, precision);
        Assert.Equal(gammaA, gammaN, precision);
        Assert.Equal(speedA, speedN, precision);
        Assert.Equal(thetaA, thetaN, 3);
        Assert.Equal(charmA, charmN, precision);
        Assert.Equal(colorA, colorN, precision);
        Assert.Equal(vegaA, vegaN, precision);
        Assert.Equal(vannaA, vannaN, precision);
        Assert.Equal(zommaA, zommaN, precision);
        Assert.Equal(rhoA, rhoN, 5);
    }
}
