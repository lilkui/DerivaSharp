using DerivaSharp.Instruments;
using DerivaSharp.Models;
using DerivaSharp.Numerics;
using static System.Math;

namespace DerivaSharp.PricingEngines;

/// <summary>
///     Pricing engine for geometric average Asian options using closed-form analytical formulas.
/// </summary>
public sealed class GeometricAverageAsianEngine : BsmPricingEngine<GeometricAverageOption>
{
    protected override double CalculateValue(GeometricAverageOption option, BsmModelParameters model, double assetPrice, DateOnly valuationDate)
    {
        double x = option.StrikePrice;
        int z = (int)option.OptionType;
        double tau = GetYearsToExpiration(option, valuationDate);
        double vol = model.Volatility;
        double r = model.RiskFreeRate;
        double b = r - model.DividendYield;

        double adjVol = vol / Sqrt(3);
        double adjB = 0.5 * (b - vol * vol / 6);

        double d1 = (Log(assetPrice / x) + (adjB + adjVol * adjVol / 2) * tau) / (adjVol * Sqrt(tau));
        double d2 = d1 - adjVol * Sqrt(tau);

        return z * (assetPrice * Exp((adjB - r) * tau) * StandardNormalDistribution.Cdf(z * d1) - x * Exp(-r * tau) * StandardNormalDistribution.Cdf(z * d2));
    }
}
