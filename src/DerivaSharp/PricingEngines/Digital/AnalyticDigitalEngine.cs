using CommunityToolkit.Diagnostics;
using DerivaSharp.Instruments;
using MathNet.Numerics.Distributions;

namespace DerivaSharp.PricingEngines;

public sealed class AnalyticDigitalEngine : PricingEngine<DigitalOption>
{
    protected override double CalculateValue(DigitalOption option, PricingContext context)
    {
        double x = option.StrikePrice;
        int z = (int)option.OptionType;
        double s = context.AssetPrice;
        double tau = GetYearsToExpiration(option, context);
        double vol = context.Volatility;
        double r = context.RiskFreeRate;
        double q = context.DividendYield;

        if (tau == 0)
        {
            bool inTheMoney = z > 0 ? s > x : s < x;
            return option switch
            {
                CashOrNothingOption con => inTheMoney ? con.Rebate : 0.0,
                AssetOrNothingOption _ => inTheMoney ? s : 0.0,
                _ => ThrowHelper.ThrowArgumentException<double>("Unknown digital option type."),
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
                return ThrowHelper.ThrowArgumentException<double>("Unknown digital option type.");
        }
    }
}
