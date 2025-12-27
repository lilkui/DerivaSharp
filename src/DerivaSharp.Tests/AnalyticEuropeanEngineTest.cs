using DerivaSharp.Instruments;
using DerivaSharp.Models;
using DerivaSharp.PricingEngines;

namespace DerivaSharp.Tests;

public class AnalyticEuropeanEngineTest
{
    private readonly EuropeanOption _call;
    private readonly EuropeanOption _put;
    private readonly BsmModel _model;
    private readonly MarketData _market;
    private readonly PricingContext _ctx;
    private readonly AnalyticEuropeanEngine _engine;

    public AnalyticEuropeanEngineTest()
    {
        DateOnly effectiveDate = new(2025, 1, 6);
        DateOnly expirationDate = effectiveDate.AddDays(365);
        _call = new EuropeanOption(OptionType.Call, 100, effectiveDate, expirationDate);
        _put = new EuropeanOption(OptionType.Put, 100, effectiveDate, expirationDate);
        _model = new BsmModel(0.3, 0.04, 0.01);
        _market = new MarketData(100);
        _ctx = new PricingContext(effectiveDate);
        _engine = new AnalyticEuropeanEngine();
    }

    public static TheoryData<string, OptionType, double> GreekTestData => new()
    {
        // Greek, OptionType, Expected
        { "Delta", OptionType.Call, 0.592749 },
        { "Delta", OptionType.Put, -0.397301 },
        { "Gamma", OptionType.Call, 0.012761 },
        { "Gamma", OptionType.Put, 0.012761 },
        { "Speed", OptionType.Call, -0.000234 },
        { "Speed", OptionType.Put, -0.000234 },
        { "Theta", OptionType.Call, -0.019163 },
        { "Theta", OptionType.Put, -0.011346 },
        { "Vega", OptionType.Call, 0.382821 },
        { "Vega", OptionType.Put, 0.382821 },
        { "Vanna", OptionType.Call, 0.000638 },
        { "Vanna", OptionType.Put, 0.000638 },
        { "Zomma", OptionType.Call, -0.000431 },
        { "Zomma", OptionType.Put, -0.000431 },
        { "Rho", OptionType.Call, 0.461238 },
        { "Rho", OptionType.Put, -0.499552 },
    };

    [Theory]
    [InlineData(OptionType.Call, 13.151137)]
    [InlineData(OptionType.Put, 10.225098)]
    public void Value_IsAccurate(OptionType optionType, double expected)
    {
        const int precision = 6;
        EuropeanOption option = optionType == OptionType.Call ? _call : _put;
        Assert.Equal(expected, _engine.Value(option, _model, _market, _ctx), precision);
    }

    [Theory]
    [MemberData(nameof(GreekTestData))]
    public void Greek_IsAccurate(string greek, OptionType optionType, double expected)
    {
        const int precision = 6;
        EuropeanOption option = optionType == OptionType.Call ? _call : _put;
        double actual = greek switch
        {
            "Delta" => _engine.Delta(option, _model, _market, _ctx),
            "Gamma" => _engine.Gamma(option, _model, _market, _ctx),
            "Speed" => _engine.Speed(option, _model, _market, _ctx),
            "Theta" => _engine.Theta(option, _model, _market, _ctx),
            "Vega" => _engine.Vega(option, _model, _market, _ctx),
            "Vanna" => _engine.Vanna(option, _model, _market, _ctx),
            "Zomma" => _engine.Zomma(option, _model, _market, _ctx),
            "Rho" => _engine.Rho(option, _model, _market, _ctx),
            _ => throw new ArgumentException("Invalid Greek"),
        };
        Assert.Equal(expected, actual, precision);
    }

    [Theory]
    [InlineData(13.1511, 0.3)]
    [InlineData(5, 0.0857)]
    [InlineData(90, 3.3664)]
    public void ImpliedVolatility_IsAccurate(double price, double expected)
    {
        const int precision = 4;
        Assert.Equal(expected, _engine.ImpliedVolatility(_call, _model, _market, _ctx, price), precision);
    }

    [Fact]
    public void Value_AtExpiry_ReturnsIntrinsicValue()
    {
        const int precision = 6;
        PricingContext ctx = _ctx with { ValuationDate = _call.ExpirationDate };
        MarketData market = _market with { AssetPrice = 110 };
        Assert.Equal(10, _engine.Value(_call, _model, market, ctx), precision);
        Assert.Equal(0, _engine.Value(_put, _model, market, ctx), precision);
    }

    [Theory]
    [InlineData(OptionType.Call)]
    [InlineData(OptionType.Put)]
    public void NumericalAndAnalyticalGreeks_AreEqual(OptionType optionType)
    {
        const int precision = 6;
        EuropeanOption option = optionType == OptionType.Call ? _call : _put;

        // Numerical Greeks
        _engine.UseNumericalGreeks = true;
        double deltaN = _engine.Delta(option, _model, _market, _ctx);
        double gammaN = _engine.Gamma(option, _model, _market, _ctx);
        double speedN = _engine.Speed(option, _model, _market, _ctx);
        double thetaN = _engine.Theta(option, _model, _market, _ctx);
        double charmN = _engine.Charm(option, _model, _market, _ctx);
        double colorN = _engine.Color(option, _model, _market, _ctx);
        double vegaN = _engine.Vega(option, _model, _market, _ctx);
        double vannaN = _engine.Vanna(option, _model, _market, _ctx);
        double zommaN = _engine.Zomma(option, _model, _market, _ctx);
        double rhoN = _engine.Rho(option, _model, _market, _ctx);

        // Analytical Greeks
        _engine.UseNumericalGreeks = false;
        double deltaA = _engine.Delta(option, _model, _market, _ctx);
        double gammaA = _engine.Gamma(option, _model, _market, _ctx);
        double speedA = _engine.Speed(option, _model, _market, _ctx);
        double thetaA = _engine.Theta(option, _model, _market, _ctx);
        double charmA = _engine.Charm(option, _model, _market, _ctx);
        double colorA = _engine.Color(option, _model, _market, _ctx);
        double vegaA = _engine.Vega(option, _model, _market, _ctx);
        double vannaA = _engine.Vanna(option, _model, _market, _ctx);
        double zommaA = _engine.Zomma(option, _model, _market, _ctx);
        double rhoA = _engine.Rho(option, _model, _market, _ctx);

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
