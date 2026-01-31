using MathNet.Numerics.Distributions;
using static System.Math;

namespace DerivaSharp.PricingEngines;

internal static class BsmCalculator
{
    public static double CalculateValue(int z, double spot, double strike, double tau, double vol, double r, double q)
    {
        if (tau == 0)
        {
            return Max(z * (spot - strike), 0);
        }

        if (vol == 0)
        {
            double forward = spot * Exp((r - q) * tau);
            return Max(z * (forward - strike), 0) * Exp(-r * tau);
        }

        double sqrtT = Sqrt(tau);
        double d1 = (Log(spot / strike) + (r - q + vol * vol / 2) * tau) / (vol * sqrtT);
        double d2 = d1 - vol * sqrtT;

        return z * (spot * Exp(-q * tau) * StdNormCdf(z * d1) - strike * Exp(-r * tau) * StdNormCdf(z * d2));
    }

    public static (double D1, double D2) D1D2(double spot, double strike, double tau, double vol, double r, double q)
    {
        double sqrtT = Sqrt(tau);
        double d1 = (Log(spot / strike) + (r - q + vol * vol / 2) * tau) / (vol * sqrtT);
        double d2 = d1 - vol * sqrtT;

        return (d1, d2);
    }

    public static double StdNormCdf(double x) => Normal.CDF(0, 1, x);

    public static double StdNormPdf(double x) => Normal.PDF(0, 1, x);
}
