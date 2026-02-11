using DerivaSharp.Instruments;
using DerivaSharp.Models;
using static System.Math;

namespace DerivaSharp.PricingEngines;

/// <summary>
///     Pricing engine for arithmetic average Asian options using the Turnbull-Wakeman approximation.
/// </summary>
public sealed class ArithmeticAverageAsianEngine : BsmPricingEngine<ArithmeticAverageOption>
{
    protected override double CalculateValue(ArithmeticAverageOption option, BsmModelParameters model, double assetPrice, DateOnly valuationDate)
    {
        double strike = option.StrikePrice;
        int z = (int)option.OptionType;
        double tau = GetYearsToExpiration(option, valuationDate);
        double r = model.RiskFreeRate;
        double q = model.DividendYield;
        double b = r - q;
        double vol = model.Volatility;
        double realizedAverage = option.RealizedAveragePrice;

        if (tau <= 0)
        {
            return Max(z * (realizedAverage - strike), 0);
        }

        double averagePeriod = GetYearsBetween(option.AverageStartDate, option.ExpirationDate);
        if (averagePeriod <= 0)
        {
            return BsmCalculator.CalculateValue(z, assetPrice, strike, tau, vol, r, q);
        }

        double t1 = Max(0, tau - averagePeriod);
        double remainingAverageTime = averagePeriod - tau;

        double m1 = b == 0
            ? 1.0
            : (Exp(b * tau) - Exp(b * t1)) / (b * (tau - t1));

        if (remainingAverageTime > 0)
        {
            double adjustedStrikeForEarlyCheck = averagePeriod / tau * strike - remainingAverageTime / tau * realizedAverage;
            if (adjustedStrikeForEarlyCheck < 0)
            {
                if (z == 1)
                {
                    double expectedAverage = realizedAverage * (averagePeriod - tau) / averagePeriod + assetPrice * m1 * tau / averagePeriod;
                    return Max(expectedAverage - strike, 0) * Exp(-r * tau);
                }

                return 0;
            }
        }

        double bA = Log(m1) / tau;
        double vA;

        if (vol == 0)
        {
            vA = 0;
        }
        else
        {
            double m2 = CalculateAverageSecondMoment(b, vol, tau, t1);
            vA = Sqrt(Log(m2) / tau - 2 * bA);
        }

        double adjustedStrike = strike;
        double scale = 1.0;
        if (remainingAverageTime > 0)
        {
            adjustedStrike = averagePeriod / tau * strike - remainingAverageTime / tau * realizedAverage;
            scale = tau / averagePeriod;
        }

        return scale * BsmCalculator.CalculateValue(z, assetPrice, adjustedStrike, tau, vA, r, r - bA);
    }

    private static double CalculateAverageSecondMoment(double b, double vol, double tau, double t1)
    {
        double vol2 = vol * vol;
        double delta = tau - t1;
        double delta2 = delta * delta;

        if (b == 0)
        {
            double vol4 = Pow(vol, 4);
            return 2 * Exp(vol2 * tau) / (vol4 * delta2) - 2 * Exp(vol2 * t1) * (1 + vol2 * delta) / (vol4 * delta2);
        }

        double twoBPlusVol2 = 2 * b + vol2;
        double bPlusVol2 = b + vol2;

        return 2 * Exp(twoBPlusVol2 * tau) / (bPlusVol2 * twoBPlusVol2 * delta2) + 2 * Exp(twoBPlusVol2 * t1) / (b * delta2) * (1 / twoBPlusVol2 - Exp(b * delta) / bPlusVol2);
    }

    private static double GetYearsBetween(DateOnly startDate, DateOnly endDate) =>
        (endDate.DayNumber - startDate.DayNumber) / 365.0;
}
