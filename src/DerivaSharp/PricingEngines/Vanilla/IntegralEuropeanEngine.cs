using DerivaSharp.Instruments;
using DerivaSharp.Models;
using MathNet.Numerics.Integration;
using static System.Math;

namespace DerivaSharp.PricingEngines;

public sealed class IntegralEuropeanEngine : BsmPricingEngine<EuropeanOption>
{
    protected override double CalculateValue(EuropeanOption option, BsmModel model, MarketData market, PricingContext context)
    {
        double x = option.StrikePrice;
        double sgn = (int)option.OptionType;
        double s = market.AssetPrice;
        double tau = GetYearsToExpiration(option, context);
        double vol = model.Volatility;
        double r = model.RiskFreeRate;
        double q = model.DividendYield;

        if (tau == 0)
        {
            return Max(sgn * (s - x), 0);
        }

        double result = DoubleExponentialTransformation.Integrate(Integrand, -8, 8, 1e-8);
        return Exp(-r * tau) * result;

        double Integrand(double z)
        {
            double st = s * Exp((r - q - 0.5 * vol * vol) * tau + vol * Sqrt(tau) * z);
            double payoff = Max(sgn * (st - x), 0);
            double pdf = Exp(-0.5 * z * z) / Sqrt(2 * PI);
            return payoff * pdf;
        }
    }
}
