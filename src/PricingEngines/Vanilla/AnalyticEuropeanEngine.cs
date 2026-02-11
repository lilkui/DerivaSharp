using DerivaSharp.Instruments;
using DerivaSharp.Models;
using static System.Math;

namespace DerivaSharp.PricingEngines;

/// <summary>
///     Pricing engine for European options using the Black-Scholes-Merton closed-form solution.
/// </summary>
public sealed class AnalyticEuropeanEngine : BsmPricingEngine<EuropeanOption>
{
    /// <summary>
    ///     Gets or sets a value indicating whether to use numerical differentiation for Greeks instead of analytical formulas.
    /// </summary>
    public bool UseNumericalGreeks { get; set; } = false;

    public override double Delta(EuropeanOption option, PricingContext<BsmModelParameters> context)
    {
        if (UseNumericalGreeks)
        {
            return base.Delta(option, context);
        }

        ValidateArguments(option, context);

        BsmModelParameters parameters = context.ModelParameters;
        double x = option.StrikePrice;
        int z = (int)option.OptionType;
        double s = context.AssetPrice;
        double tau = GetYearsToExpiration(option, context.ValuationDate);
        double vol = parameters.Volatility;
        double r = parameters.RiskFreeRate;
        double q = parameters.DividendYield;

        (double d1, double _) = BsmCalculator.D1D2(s, x, tau, vol, r, q);
        return z * Exp(-q * tau) * BsmCalculator.StdNormCdf(z * d1);
    }

    public override double Gamma(EuropeanOption option, PricingContext<BsmModelParameters> context)
    {
        ValidateArguments(option, context);

        if (UseNumericalGreeks)
        {
            return base.Gamma(option, context);
        }

        BsmModelParameters parameters = context.ModelParameters;
        double x = option.StrikePrice;
        double s = context.AssetPrice;
        double tau = GetYearsToExpiration(option, context.ValuationDate);
        double vol = parameters.Volatility;
        double r = parameters.RiskFreeRate;
        double q = parameters.DividendYield;

        (double d1, double _) = BsmCalculator.D1D2(s, x, tau, vol, r, q);
        return Exp(-q * tau) * BsmCalculator.StdNormPdf(d1) / (s * vol * Sqrt(tau));
    }

    public override double Speed(EuropeanOption option, PricingContext<BsmModelParameters> context)
    {
        if (UseNumericalGreeks)
        {
            return base.Speed(option, context);
        }

        ValidateArguments(option, context);

        BsmModelParameters parameters = context.ModelParameters;
        double x = option.StrikePrice;
        double s = context.AssetPrice;
        double tau = GetYearsToExpiration(option, context.ValuationDate);
        double vol = parameters.Volatility;
        double r = parameters.RiskFreeRate;
        double q = parameters.DividendYield;

        (double d1, double _) = BsmCalculator.D1D2(s, x, tau, vol, r, q);
        return -Gamma(option, context) * (1 + d1 / (vol * Sqrt(tau))) / s;
    }

    public override double Theta(EuropeanOption option, PricingContext<BsmModelParameters> context)
    {
        if (UseNumericalGreeks)
        {
            return base.Theta(option, context);
        }

        ValidateArguments(option, context);

        BsmModelParameters parameters = context.ModelParameters;
        double x = option.StrikePrice;
        int z = (int)option.OptionType;
        double s = context.AssetPrice;
        double tau = GetYearsToExpiration(option, context.ValuationDate);
        double vol = parameters.Volatility;
        double r = parameters.RiskFreeRate;
        double q = parameters.DividendYield;

        (double d1, double d2) = BsmCalculator.D1D2(s, x, tau, vol, r, q);
        double theta = -s * Exp(-q * tau) * BsmCalculator.StdNormPdf(d1) * vol / (2 * Sqrt(tau)) + z * q * s * Exp(-q * tau) * BsmCalculator.StdNormCdf(z * d1) -
                       z * r * x * Exp(-r * tau) * BsmCalculator.StdNormCdf(z * d2);
        return theta / 365;
    }

    public override double Charm(EuropeanOption option, PricingContext<BsmModelParameters> context)
    {
        if (UseNumericalGreeks)
        {
            return base.Charm(option, context);
        }

        ValidateArguments(option, context);

        BsmModelParameters parameters = context.ModelParameters;
        double x = option.StrikePrice;
        int z = (int)option.OptionType;
        double s = context.AssetPrice;
        double tau = GetYearsToExpiration(option, context.ValuationDate);
        double vol = parameters.Volatility;
        double r = parameters.RiskFreeRate;
        double q = parameters.DividendYield;

        (double d1, double d2) = BsmCalculator.D1D2(s, x, tau, vol, r, q);
        return -Exp(-q * tau) * (BsmCalculator.StdNormPdf(d1) * ((r - q) / (vol * Sqrt(tau)) - 0.5 * d2 / tau) - z * q * BsmCalculator.StdNormCdf(z * d1)) / 365;
    }

    public override double Color(EuropeanOption option, PricingContext<BsmModelParameters> context)
    {
        if (UseNumericalGreeks)
        {
            return base.Color(option, context);
        }

        ValidateArguments(option, context);

        BsmModelParameters parameters = context.ModelParameters;
        double x = option.StrikePrice;
        double s = context.AssetPrice;
        double tau = GetYearsToExpiration(option, context.ValuationDate);
        double vol = parameters.Volatility;
        double r = parameters.RiskFreeRate;
        double q = parameters.DividendYield;

        (double d1, double d2) = BsmCalculator.D1D2(s, x, tau, vol, r, q);
        return Gamma(option, context) * (q + (r - q) * d1 / (vol * Sqrt(tau)) + (1 - d1 * d2) / (2 * tau)) / 365;
    }

    public override double Vega(EuropeanOption option, PricingContext<BsmModelParameters> context)
    {
        if (UseNumericalGreeks)
        {
            return base.Vega(option, context);
        }

        ValidateArguments(option, context);

        BsmModelParameters parameters = context.ModelParameters;
        double x = option.StrikePrice;
        double s = context.AssetPrice;
        double tau = GetYearsToExpiration(option, context.ValuationDate);
        double vol = parameters.Volatility;
        double r = parameters.RiskFreeRate;
        double q = parameters.DividendYield;

        (double d1, double _) = BsmCalculator.D1D2(s, x, tau, vol, r, q);
        return s * Exp(-q * tau) * BsmCalculator.StdNormPdf(d1) * Sqrt(tau) / 100;
    }

    public override double Vanna(EuropeanOption option, PricingContext<BsmModelParameters> context)
    {
        if (UseNumericalGreeks)
        {
            return base.Vanna(option, context);
        }

        ValidateArguments(option, context);

        BsmModelParameters parameters = context.ModelParameters;
        double x = option.StrikePrice;
        double s = context.AssetPrice;
        double tau = GetYearsToExpiration(option, context.ValuationDate);
        double vol = parameters.Volatility;
        double r = parameters.RiskFreeRate;
        double q = parameters.DividendYield;

        (double d1, double d2) = BsmCalculator.D1D2(s, x, tau, vol, r, q);
        return -Exp(-q * tau) * d2 / vol * BsmCalculator.StdNormPdf(d1) / 100;
    }

    public override double Zomma(EuropeanOption option, PricingContext<BsmModelParameters> context)
    {
        if (UseNumericalGreeks)
        {
            return base.Zomma(option, context);
        }

        ValidateArguments(option, context);

        BsmModelParameters parameters = context.ModelParameters;
        double x = option.StrikePrice;
        double s = context.AssetPrice;
        double tau = GetYearsToExpiration(option, context.ValuationDate);
        double vol = parameters.Volatility;
        double r = parameters.RiskFreeRate;
        double q = parameters.DividendYield;

        (double d1, double d2) = BsmCalculator.D1D2(s, x, tau, vol, r, q);
        return Gamma(option, context) * (d1 * d2 - 1) / vol / 100;
    }

    public override double Rho(EuropeanOption option, PricingContext<BsmModelParameters> context)
    {
        if (UseNumericalGreeks)
        {
            return base.Rho(option, context);
        }

        ValidateArguments(option, context);

        BsmModelParameters parameters = context.ModelParameters;
        double x = option.StrikePrice;
        int z = (int)option.OptionType;
        double s = context.AssetPrice;
        double tau = GetYearsToExpiration(option, context.ValuationDate);
        double vol = parameters.Volatility;
        double r = parameters.RiskFreeRate;
        double q = parameters.DividendYield;

        (double _, double d2) = BsmCalculator.D1D2(s, x, tau, vol, r, q);
        return z * tau * x * Exp(-r * tau) * BsmCalculator.StdNormCdf(z * d2) / 100;
    }

    public override PricingResult ValueAndGreeks(EuropeanOption option, PricingContext<BsmModelParameters> context)
    {
        if (UseNumericalGreeks)
        {
            return base.ValueAndGreeks(option, context);
        }

        ValidateArguments(option, context);

        BsmModelParameters parameters = context.ModelParameters;
        double x = option.StrikePrice;
        int z = (int)option.OptionType;
        double s = context.AssetPrice;
        double tau = GetYearsToExpiration(option, context.ValuationDate);
        double vol = parameters.Volatility;
        double r = parameters.RiskFreeRate;
        double q = parameters.DividendYield;

        double value;
        double delta = 0, gamma = 0, speed = 0, theta = 0, charm = 0, color = 0, vega = 0, vanna = 0, zomma = 0, rho = 0;

        if (tau <= 0)
        {
            value = Max(z * (s - x), 0);
        }
        else
        {
            (double d1, double d2) = BsmCalculator.D1D2(s, x, tau, vol, r, q);

            double sqrtT = Sqrt(tau);
            double expQt = Exp(-q * tau);
            double expRt = Exp(-r * tau);
            double nd1 = BsmCalculator.StdNormPdf(d1);
            double nzd1 = BsmCalculator.StdNormCdf(z * d1);
            double nzd2 = BsmCalculator.StdNormCdf(z * d2);

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

    protected override double CalculateValue(EuropeanOption option, BsmModelParameters parameters, double assetPrice, DateOnly valuationDate)
    {
        PricingContext<BsmModelParameters> context = new(parameters, assetPrice, valuationDate);
        ValidateArguments(option, context);

        double x = option.StrikePrice;
        int z = (int)option.OptionType;
        double tau = GetYearsToExpiration(option, context.ValuationDate);
        double vol = parameters.Volatility;
        double r = parameters.RiskFreeRate;
        double q = parameters.DividendYield;

        return BsmCalculator.CalculateValue(z, assetPrice, x, tau, vol, r, q);
    }
}
