using DerivaSharp.Instruments;
using DerivaSharp.Numerics;
using MathNet.Numerics.Distributions;
using static System.Math;

namespace DerivaSharp.PricingEngines;

public sealed class BjerksundStenslandAmericanEngine : PricingEngine<AmericanOption>
{
    protected override double CalculateValue(AmericanOption option, PricingContext context)
    {
        if (context.ValuationDate == option.ExpirationDate)
        {
            double z = (int)option.OptionType;
            return Max(z * (context.AssetPrice - option.StrikePrice), 0);
        }

        if (option.OptionType == OptionType.Call)
        {
            return AmericanCallValue(option, context);
        }

        // Use the put-call transformation for American put options
        AmericanOption transformedOption = new(OptionType.Call, context.AssetPrice, option.EffectiveDate, option.ExpirationDate);
        return AmericanCallValue(
            transformedOption,
            context with { AssetPrice = option.StrikePrice, RiskFreeRate = context.DividendYield, DividendYield = context.RiskFreeRate });
    }

    private static double Ksi(double s, double t2, double gamma, double h, double i2, double i1, double t1, double r, double b, double v)
    {
        double e1 = (Log(s / i1) + (b + (gamma - 0.5) * v * v) * t1) / (v * Sqrt(t1));
        double e2 = (Log(i2 * i2 / (s * i1)) + (b + (gamma - 0.5) * v * v) * t1) / (v * Sqrt(t1));
        double e3 = (Log(s / i1) - (b + (gamma - 0.5) * v * v) * t1) / (v * Sqrt(t1));
        double e4 = (Log(i2 * i2 / (s * i1)) - (b + (gamma - 0.5) * v * v) * t1) / (v * Sqrt(t1));

        double f1 = (Log(s / h) + (b + (gamma - 0.5) * v * v) * t2) / (v * Sqrt(t2));
        double f2 = (Log(i2 * i2 / (s * h)) + (b + (gamma - 0.5) * v * v) * t2) / (v * Sqrt(t2));
        double f3 = (Log(i1 * i1 / (s * h)) + (b + (gamma - 0.5) * v * v) * t2) / (v * Sqrt(t2));
        double f4 = (Log(s * i1 * i1 / (h * i2 * i2)) + (b + (gamma - 0.5) * v * v) * t2) / (v * Sqrt(t2));

        double rho = Sqrt(t1 / t2);
        double lambda = -r + gamma * b + 0.5 * gamma * (gamma - 1) * v * v;
        double kappa = 2 * b / (v * v) + (2 * gamma - 1);

        double ksi = Exp(lambda * t2) * Pow(s, gamma) * (BivariateNormalDistribution.Cdf(-e1, -f1, rho) -
                                                         Pow(i2 / s, kappa) * BivariateNormalDistribution.Cdf(-e2, -f2, rho) -
                                                         Pow(i1 / s, kappa) * BivariateNormalDistribution.Cdf(-e3, -f3, -rho) +
                                                         Pow(i1 / i2, kappa) * BivariateNormalDistribution.Cdf(-e4, -f4, -rho));

        return ksi;
    }

    private static double Phi(double s, double t, double gamma, double h, double i, double r, double b, double v)
    {
        double lambda = (-r + gamma * b + 0.5 * gamma * (gamma - 1) * v * v) * t;
        double d = -(Log(s / h) + (b + (gamma - 0.5) * v * v) * t) / (v * Sqrt(t));
        double kappa = 2 * b / (v * v) + 2 * gamma - 1;
        double phi = Exp(lambda) * Pow(s, gamma) * (StdNormCdf(d) - Pow(i / s, kappa) * StdNormCdf(d - 2 * Log(i / s) / (v * Sqrt(t))));

        return phi;
    }

    private static double StdNormCdf(double x) => Normal.CDF(0, 1, x);

    private double AmericanCallValue(AmericanOption option, PricingContext context)
    {
        double x = option.StrikePrice;
        double s = context.AssetPrice;
        double tau = GetYearsToExpiration(option, context);
        double vol = context.Volatility;
        double r = context.RiskFreeRate;
        double q = context.DividendYield;
        double b = r - q;
        double t1 = 0.5 * (Sqrt(5) - 1) * tau;

        double value;

        if (q <= 0.0)
        {
            // Never optimal to exercise early
            EuropeanOption europeanOption = new(OptionType.Call, x, option.EffectiveDate, option.ExpirationDate);
            AnalyticEuropeanEngine europeanEngine = new();
            value = europeanEngine.Value(europeanOption, context);
            return value;
        }

        double beta = 0.5 - b / vol / vol + Sqrt(Pow(b / vol / vol - 0.5, 2) + 2 * r / vol / vol);
        double bInfinity = beta / (beta - 1) * x;
        double b0 = Max(x, r / (r - b) * x);
        double ht1 = -(b * t1 + 2 * vol * Sqrt(t1)) * x * x / ((bInfinity - b0) * b0);
        double ht2 = -(b * tau + 2 * vol * Sqrt(tau)) * x * x / ((bInfinity - b0) * b0);
        double i1 = b0 + (bInfinity - b0) * (1 - Exp(ht1));
        double i2 = b0 + (bInfinity - b0) * (1 - Exp(ht2));
        double alfa1 = (i1 - x) * Pow(i1, -beta);
        double alfa2 = (i2 - x) * Pow(i2, -beta);

        if (s >= i2)
        {
            return s - x;
        }

        value = alfa2 * Pow(s, beta) - alfa2 * Phi(s, t1, beta, i2, i2, r, b, vol) + Phi(s, t1, 1, i2, i2, r, b, vol) - Phi(s, t1, 1, i1, i2, r, b, vol) -
            x * Phi(s, t1, 0, i2, i2, r, b, vol) + x * Phi(s, t1, 0, i1, i2, r, b, vol) + alfa1 * Phi(s, t1, beta, i1, i2, r, b, vol) -
            alfa1 * Ksi(s, tau, beta, i1, i2, i1, t1, r, b, vol) + Ksi(s, tau, 1, i1, i2, i1, t1, r, b, vol) - Ksi(s, tau, 1, x, i2, i1, t1, r, b, vol) -
            x * Ksi(s, tau, 0, i1, i2, i1, t1, r, b, vol) + x * Ksi(s, tau, 0, x, i2, i1, t1, r, b, vol);

        return value;
    }
}
