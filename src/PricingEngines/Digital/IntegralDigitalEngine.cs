using CommunityToolkit.Diagnostics;
using DerivaSharp.Instruments;
using DerivaSharp.Models;
using MathNet.Numerics.Integration;
using static System.Math;

namespace DerivaSharp.PricingEngines;

public sealed class IntegralDigitalEngine : BsmPricingEngine<DigitalOption>
{
    protected override double CalculateValue(DigitalOption option, BsmModelParameters parameters, double assetPrice, DateOnly valuationDate)
    {
        double x = option.StrikePrice;
        int sign = (int)option.OptionType;
        double tau = GetYearsToExpiration(option, valuationDate);
        double vol = parameters.Volatility;
        double r = parameters.RiskFreeRate;
        double q = parameters.DividendYield;

        if (tau <= 0)
        {
            bool inTheMoney = sign > 0 ? assetPrice > x : assetPrice < x;
            return option switch
            {
                CashOrNothingOption con => inTheMoney ? con.Rebate : 0.0,
                AssetOrNothingOption _ => inTheMoney ? assetPrice : 0.0,
                _ => ThrowHelper.ThrowArgumentException<double>(ExceptionMessages.InvalidDigitalOption),
            };
        }

        double zStar = (Log(x / assetPrice) - (r - q - 0.5 * vol * vol) * tau) / (vol * Sqrt(tau));

        const double zLower = -10.0;
        const double zUpper = 10.0;

        (double a, double b) = sign > 0
            ? (Max(zStar, zLower), zUpper)
            : (zLower, Min(zStar, zUpper));

        if (a >= b)
        {
            return 0.0;
        }

        double result = DoubleExponentialTransformation.Integrate(Integrand, a, b, 1e-8);
        return Exp(-r * tau) * result;

        double Integrand(double z)
        {
            double st = assetPrice * Exp((r - q - 0.5 * vol * vol) * tau + vol * Sqrt(tau) * z);
            double payoff = option switch
            {
                CashOrNothingOption con => con.Rebate,
                AssetOrNothingOption _ => st,
                _ => ThrowHelper.ThrowArgumentException<double>(ExceptionMessages.InvalidDigitalOption),
            };
            double pdf = Exp(-0.5 * z * z) / Sqrt(2 * PI);
            return payoff * pdf;
        }
    }
}
