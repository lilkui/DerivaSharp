using DerivaSharp.Instruments;
using DerivaSharp.Models;
using DerivaSharp.Numerics;
using static System.Math;

namespace DerivaSharp.PricingEngines;

/// <summary>
///     Pricing engine for European options using numerical integration of the risk-neutral expectation.
/// </summary>
public sealed class IntegralEuropeanEngine : BsmPricingEngine<EuropeanOption>
{
    protected override double CalculateValue(EuropeanOption option, BsmModelParameters parameters, double assetPrice, DateOnly valuationDate)
    {
        double x = option.StrikePrice;
        double sgn = (int)option.OptionType;
        double tau = GetYearsToExpiration(option, valuationDate);
        double vol = parameters.Volatility;
        double r = parameters.RiskFreeRate;
        double q = parameters.DividendYield;

        if (tau <= 0)
        {
            return Max(sgn * (assetPrice - x), 0);
        }

        double zStar = (Log(x / assetPrice) - (r - q - 0.5 * vol * vol) * tau) / (vol * Sqrt(tau));

        const double zLower = -10.0;
        const double zUpper = 10.0;

        (double a, double b) = sgn > 0
            ? (Max(zStar, zLower), zUpper)
            : (zLower, Min(zStar, zUpper));

        if (a >= b)
        {
            return 0.0;
        }

        const int gaussLegendreOrder = 32;
        double result = GaussLegendreQuadrature.Integrate(Integrand, a, b, gaussLegendreOrder);
        return Exp(-r * tau) * result;

        double Integrand(double z)
        {
            double st = assetPrice * Exp((r - q - 0.5 * vol * vol) * tau + vol * Sqrt(tau) * z);
            double payoff = sgn * (st - x);
            double pdf = Exp(-0.5 * z * z) / Sqrt(2 * PI);
            return payoff * pdf;
        }
    }
}
