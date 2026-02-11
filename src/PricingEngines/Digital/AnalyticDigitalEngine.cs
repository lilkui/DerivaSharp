using CommunityToolkit.Diagnostics;
using DerivaSharp.Instruments;
using DerivaSharp.Models;
using MathNet.Numerics.Distributions;

namespace DerivaSharp.PricingEngines;

/// <summary>
///     Pricing engine for digital options using closed-form analytical formulas.
/// </summary>
public sealed class AnalyticDigitalEngine : BsmPricingEngine<DigitalOption>
{
    protected override double CalculateValue(DigitalOption option, BsmModelParameters parameters, double assetPrice, DateOnly valuationDate)
    {
        double x = option.StrikePrice;
        int z = (int)option.OptionType;
        double tau = GetYearsToExpiration(option, valuationDate);
        double vol = parameters.Volatility;
        double r = parameters.RiskFreeRate;
        double q = parameters.DividendYield;

        if (tau <= 0)
        {
            bool inTheMoney = z > 0 ? assetPrice > x : assetPrice < x;
            return option switch
            {
                CashOrNothingOption con => inTheMoney ? con.Rebate : 0.0,
                AssetOrNothingOption _ => inTheMoney ? assetPrice : 0.0,
                _ => ThrowHelper.ThrowArgumentException<double>(ExceptionMessages.InvalidDigitalOption),
            };
        }

        switch (option)
        {
            case CashOrNothingOption con:
            {
                double k = con.Rebate;
                double d = (Math.Log(assetPrice / x) + (r - q - 0.5 * vol * vol) * tau) / (vol * Math.Sqrt(tau));
                return k * Math.Exp(-r * tau) * Normal.CDF(0, 1, z * d);
            }

            case AssetOrNothingOption:
            {
                double d = (Math.Log(assetPrice / x) + (r - q + 0.5 * vol * vol) * tau) / (vol * Math.Sqrt(tau));
                return assetPrice * Math.Exp(-q * tau) * Normal.CDF(0, 1, z * d);
            }

            default:
                return ThrowHelper.ThrowArgumentException<double>(ExceptionMessages.InvalidDigitalOption);
        }
    }
}
