using DerivaSharp.Instruments;
using DerivaSharp.Models;
using MathNet.Numerics.Distributions;
using static System.Math;

namespace DerivaSharp.PricingEngines;

public sealed class ArithmeticAverageAsianEngine : BsmPricingEngine<ArithmeticAverageOption>
{
    protected override double CalculateValue(ArithmeticAverageOption option, BsmModelParameters model, double assetPrice, DateOnly valuationDate)
    {
        double strike = option.StrikePrice;
        int z = (int)option.OptionType;
        double tau = GetYearsToExpiration(option, valuationDate);
        double r = model.RiskFreeRate;
        double b = r - model.DividendYield;
        double vol = model.Volatility;
        double realizedAverage = option.RealizedAveragePrice;

        if (tau == 0)
        {
            return Max(z * (realizedAverage - strike), 0);
        }

        double averagePeriod = GetYearsBetween(option.AverageStartDate, option.ExpirationDate);
        if (averagePeriod == 0)
        {
            return GeneralizedBlackScholes(z, assetPrice, strike, tau, r, b, vol);
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

        return scale * GeneralizedBlackScholes(z, assetPrice, adjustedStrike, tau, r, bA, vA);
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

    private static double GeneralizedBlackScholes(int z, double s, double x, double tau, double r, double b, double vol)
    {
        if (tau == 0)
        {
            return Max(z * (s - x), 0);
        }

        if (vol == 0)
        {
            double forward = s * Exp(b * tau);
            return Max(z * (forward - x), 0) * Exp(-r * tau);
        }

        double sqrtT = Sqrt(tau);
        double d1 = (Log(s / x) + (b + vol * vol / 2) * tau) / (vol * sqrtT);
        double d2 = d1 - vol * sqrtT;

        return z * (s * Exp((b - r) * tau) * StdNormCdf(z * d1) - x * Exp(-r * tau) * StdNormCdf(z * d2));
    }

    private static double StdNormCdf(double x) => Normal.CDF(0, 1, x);

    private static double GetYearsBetween(DateOnly startDate, DateOnly endDate) =>
        (endDate.DayNumber - startDate.DayNumber) / 365.0;
}
