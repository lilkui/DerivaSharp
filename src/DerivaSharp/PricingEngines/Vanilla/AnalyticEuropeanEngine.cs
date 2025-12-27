using DerivaSharp.Instruments;
using DerivaSharp.Models;
using MathNet.Numerics.Distributions;
using static System.Math;

namespace DerivaSharp.PricingEngines;

public sealed class AnalyticEuropeanEngine : BsmPricingEngine<EuropeanOption>
{
    public bool UseNumericalGreeks { get; set; } = false;

    public override double Delta(EuropeanOption option, BsmModel model, MarketData market, PricingContext context)
    {
        if (UseNumericalGreeks)
        {
            return base.Delta(option, model, market, context);
        }

        ValidateArguments(option, context);

        double x = option.StrikePrice;
        int z = (int)option.OptionType;
        double s = market.AssetPrice;
        double tau = GetYearsToExpiration(option, context);
        double vol = model.Volatility;
        double r = model.RiskFreeRate;
        double q = model.DividendYield;

        (double d1, double _) = D1D2(x, s, tau, vol, r, q);
        return z * Exp(-q * tau) * StdNormCdf(z * d1);
    }

    public override double Gamma(EuropeanOption option, BsmModel model, MarketData market, PricingContext context)
    {
        ValidateArguments(option, context);

        if (UseNumericalGreeks)
        {
            return base.Gamma(option, model, market, context);
        }

        double x = option.StrikePrice;
        double s = market.AssetPrice;
        double tau = GetYearsToExpiration(option, context);
        double vol = model.Volatility;
        double r = model.RiskFreeRate;
        double q = model.DividendYield;

        (double d1, double _) = D1D2(x, s, tau, vol, r, q);
        return Exp(-q * tau) * StdNormPdf(d1) / (s * vol * Sqrt(tau));
    }

    public override double Speed(EuropeanOption option, BsmModel model, MarketData market, PricingContext context)
    {
        if (UseNumericalGreeks)
        {
            return base.Speed(option, model, market, context);
        }

        ValidateArguments(option, context);

        double x = option.StrikePrice;
        double s = market.AssetPrice;
        double tau = GetYearsToExpiration(option, context);
        double vol = model.Volatility;
        double r = model.RiskFreeRate;
        double q = model.DividendYield;

        (double d1, double _) = D1D2(x, s, tau, vol, r, q);
        return -Gamma(option, model, market, context) * (1 + d1 / (vol * Sqrt(tau))) / s;
    }

    public override double Theta(EuropeanOption option, BsmModel model, MarketData market, PricingContext context)
    {
        if (UseNumericalGreeks)
        {
            return base.Theta(option, model, market, context);
        }

        ValidateArguments(option, context);

        double x = option.StrikePrice;
        int z = (int)option.OptionType;
        double s = market.AssetPrice;
        double tau = GetYearsToExpiration(option, context);
        double vol = model.Volatility;
        double r = model.RiskFreeRate;
        double q = model.DividendYield;

        (double d1, double d2) = D1D2(x, s, tau, vol, r, q);
        double theta = -s * Exp(-q * tau) * StdNormPdf(d1) * vol / (2 * Sqrt(tau)) + z * q * s * Exp(-q * tau) * StdNormCdf(z * d1) -
                       z * r * x * Exp(-r * tau) * StdNormCdf(z * d2);
        return theta / 365;
    }

    public override double Charm(EuropeanOption option, BsmModel model, MarketData market, PricingContext context)
    {
        if (UseNumericalGreeks)
        {
            return base.Charm(option, model, market, context);
        }

        ValidateArguments(option, context);

        double x = option.StrikePrice;
        int z = (int)option.OptionType;
        double s = market.AssetPrice;
        double tau = GetYearsToExpiration(option, context);
        double vol = model.Volatility;
        double r = model.RiskFreeRate;
        double q = model.DividendYield;

        (double d1, double d2) = D1D2(x, s, tau, vol, r, q);
        return -Exp(-q * tau) * (StdNormPdf(d1) * ((r - q) / (vol * Sqrt(tau)) - 0.5 * d2 / tau) - z * q * StdNormCdf(z * d1)) / 365;
    }

    public override double Color(EuropeanOption option, BsmModel model, MarketData market, PricingContext context)
    {
        if (UseNumericalGreeks)
        {
            return base.Color(option, model, market, context);
        }

        ValidateArguments(option, context);

        double x = option.StrikePrice;
        double s = market.AssetPrice;
        double tau = GetYearsToExpiration(option, context);
        double vol = model.Volatility;
        double r = model.RiskFreeRate;
        double q = model.DividendYield;

        (double d1, double d2) = D1D2(x, s, tau, vol, r, q);
        return Gamma(option, model, market, context) * (q + (r - q) * d1 / (vol * Sqrt(tau)) + (1 - d1 * d2) / (2 * tau)) / 365;
    }

    public override double Vega(EuropeanOption option, BsmModel model, MarketData market, PricingContext context)
    {
        if (UseNumericalGreeks)
        {
            return base.Vega(option, model, market, context);
        }

        ValidateArguments(option, context);

        double x = option.StrikePrice;
        double s = market.AssetPrice;
        double tau = GetYearsToExpiration(option, context);
        double vol = model.Volatility;
        double r = model.RiskFreeRate;
        double q = model.DividendYield;

        (double d1, double _) = D1D2(x, s, tau, vol, r, q);
        return s * Exp(-q * tau) * StdNormPdf(d1) * Sqrt(tau) / 100;
    }

    public override double Vanna(EuropeanOption option, BsmModel model, MarketData market, PricingContext context)
    {
        if (UseNumericalGreeks)
        {
            return base.Vanna(option, model, market, context);
        }

        ValidateArguments(option, context);

        double x = option.StrikePrice;
        double s = market.AssetPrice;
        double tau = GetYearsToExpiration(option, context);
        double vol = model.Volatility;
        double r = model.RiskFreeRate;
        double q = model.DividendYield;

        (double d1, double d2) = D1D2(x, s, tau, vol, r, q);
        return -Exp(-q * tau) * d2 / vol * StdNormPdf(d1) / 100;
    }

    public override double Zomma(EuropeanOption option, BsmModel model, MarketData market, PricingContext context)
    {
        if (UseNumericalGreeks)
        {
            return base.Zomma(option, model, market, context);
        }

        ValidateArguments(option, context);

        double x = option.StrikePrice;
        double s = market.AssetPrice;
        double tau = GetYearsToExpiration(option, context);
        double vol = model.Volatility;
        double r = model.RiskFreeRate;
        double q = model.DividendYield;

        (double d1, double d2) = D1D2(x, s, tau, vol, r, q);
        return Gamma(option, model, market, context) * (d1 * d2 - 1) / vol / 100;
    }

    public override double Rho(EuropeanOption option, BsmModel model, MarketData market, PricingContext context)
    {
        if (UseNumericalGreeks)
        {
            return base.Rho(option, model, market, context);
        }

        ValidateArguments(option, context);

        double x = option.StrikePrice;
        int z = (int)option.OptionType;
        double s = market.AssetPrice;
        double tau = GetYearsToExpiration(option, context);
        double vol = model.Volatility;
        double r = model.RiskFreeRate;
        double q = model.DividendYield;

        (double _, double d2) = D1D2(x, s, tau, vol, r, q);
        return z * tau * x * Exp(-r * tau) * StdNormCdf(z * d2) / 100;
    }

    public override PricingResult ValueAndGreeks(EuropeanOption option, BsmModel model, MarketData market, PricingContext context)
    {
        if (UseNumericalGreeks)
        {
            return base.ValueAndGreeks(option, model, market, context);
        }

        ValidateArguments(option, context);

        double x = option.StrikePrice;
        int z = (int)option.OptionType;
        double s = market.AssetPrice;
        double tau = GetYearsToExpiration(option, context);
        double vol = model.Volatility;
        double r = model.RiskFreeRate;
        double q = model.DividendYield;

        double value;
        double delta = 0, gamma = 0, speed = 0, theta = 0, charm = 0, color = 0, vega = 0, vanna = 0, zomma = 0, rho = 0;

        if (tau == 0)
        {
            value = Max(z * (s - x), 0);
        }
        else
        {
            (double d1, double d2) = D1D2(x, s, tau, vol, r, q);

            double sqrtT = Sqrt(tau);
            double expQt = Exp(-q * tau);
            double expRt = Exp(-r * tau);
            double nd1 = StdNormPdf(d1);
            double nzd1 = StdNormCdf(z * d1);
            double nzd2 = StdNormCdf(z * d2);

            value = z * (s * expQt * nzd1 - x * expRt * nzd2);
            delta = z * expQt * nzd1;
            gamma = expQt * nd1 / (s * vol * sqrtT);
            speed = -gamma * (1 + d1 / (vol * sqrtT)) / s;
            theta = (-s * expQt * nd1 * vol / (2 * sqrtT) + z * q * s * expQt * nzd1 - z * r * x * expRt * nzd2) / 365;
            charm = -expQt * (nd1 * ((r - q) / (vol * sqrtT) - 0.5 * d2 / tau) - z * q * nzd1) / 365;
            color = gamma * (q + (r - q) * d1 / (vol * sqrtT) + (1 - d1 * d2) / (2 * tau)) / 365;
            vega = s * expQt * nd1 * sqrtT / 100;
            vanna = -expQt * d2 / vol * nd1 / 100;
            zomma = gamma * (d1 * d2 - 1) / vol / 100;
            rho = z * tau * x * expRt * nzd2 / 100;
        }

        return new PricingResult
        {
            Value = value,
            Delta = delta,
            Gamma = gamma,
            Speed = speed,
            Theta = theta,
            Charm = charm,
            Color = color,
            Vega = vega,
            Vanna = vanna,
            Zomma = zomma,
            Rho = rho,
        };
    }

    protected override double CalculateValue(EuropeanOption option, BsmModel model, MarketData market, PricingContext context)
    {
        ValidateArguments(option, context);

        double x = option.StrikePrice;
        int z = (int)option.OptionType;
        double s = market.AssetPrice;
        double tau = GetYearsToExpiration(option, context);
        double vol = model.Volatility;
        double r = model.RiskFreeRate;
        double q = model.DividendYield;

        if (tau == 0)
        {
            return Max(z * (s - x), 0);
        }

        (double d1, double d2) = D1D2(x, s, tau, vol, r, q);
        return z * (s * Exp(-q * tau) * StdNormCdf(z * d1) - x * Exp(-r * tau) * StdNormCdf(z * d2));
    }

    private static double StdNormCdf(double x) => Normal.CDF(0, 1, x);

    private static double StdNormPdf(double x) => Normal.PDF(0, 1, x);

    private static (double D1, double D2) D1D2(double x, double s, double tau, double vol, double r, double q)
    {
        double d1 = (Log(s / x) + (r - q + vol * vol / 2) * tau) / (vol * Sqrt(tau));
        double d2 = d1 - vol * Sqrt(tau);

        return (d1, d2);
    }
}
