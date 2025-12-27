using CommunityToolkit.Diagnostics;
using DerivaSharp.Instruments;
using DerivaSharp.Models;
using MathNet.Numerics.Distributions;

namespace DerivaSharp.PricingEngines;

public sealed class AnalyticDigitalEngine : BsmPricingEngine<DigitalOption>
{
    protected override double CalculateValue(DigitalOption option, BsmModel model, MarketData market, PricingContext context)
    {
        double x = option.StrikePrice;
        int z = (int)option.OptionType;
        double s = market.AssetPrice;
        double tau = GetYearsToExpiration(option, context);
        double vol = model.Volatility;
        double r = model.RiskFreeRate;
        double q = model.DividendYield;

        if (tau == 0)
        {
            bool inTheMoney = z > 0 ? s > x : s < x;
            return option switch
            {
                CashOrNothingOption con => inTheMoney ? con.Rebate : 0.0,
                AssetOrNothingOption _ => inTheMoney ? s : 0.0,
                _ => ThrowHelper.ThrowArgumentException<double>(ExceptionMessages.InvalidDigitalOption),
            };
        }

        switch (option)
        {
            case CashOrNothingOption con:
            {
                double k = con.Rebate;
                double d = (Math.Log(s / x) + (r - q - 0.5 * vol * vol) * tau) / (vol * Math.Sqrt(tau));
                return k * Math.Exp(-r * tau) * Normal.CDF(0, 1, z * d);
            }

            case AssetOrNothingOption:
            {
                double d = (Math.Log(s / x) + (r - q + 0.5 * vol * vol) * tau) / (vol * Math.Sqrt(tau));
                return s * Math.Exp(-q * tau) * Normal.CDF(0, 1, z * d);
            }

            default:
                return ThrowHelper.ThrowArgumentException<double>(ExceptionMessages.InvalidDigitalOption);
        }
    }
}
