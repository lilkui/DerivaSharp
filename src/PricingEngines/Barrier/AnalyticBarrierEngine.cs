using CommunityToolkit.Diagnostics;
using DerivaSharp.Instruments;
using DerivaSharp.Models;
using MathNet.Numerics.Distributions;
using static System.Math;

namespace DerivaSharp.PricingEngines;

public sealed class AnalyticBarrierEngine : BsmPricingEngine<BarrierOption>
{
    private const double Beta = 0.5825971579390107; // Correction factor for discrete barriers

    protected override double CalculateValue(BarrierOption option, BsmModelParameters parameters, double assetPrice, DateOnly valuationDate)
    {
        double x = option.StrikePrice;
        double h = option.BarrierPrice;
        double k = option.Rebate;
        double dt = option.ObservationInterval;
        double sgn = (int)option.OptionType;
        double s = assetPrice;
        double tau = GetYearsToExpiration(option, valuationDate);
        double vol = parameters.Volatility;
        double r = parameters.RiskFreeRate;
        double q = parameters.DividendYield;

        if (tau <= 0)
        {
            double intrinsic = Max(sgn * (s - x), 0);
            return option.BarrierType switch
            {
                BarrierType.UpAndIn => s >= h ? intrinsic : k,
                BarrierType.DownAndIn => s <= h ? intrinsic : k,
                BarrierType.UpAndOut => s >= h ? k : intrinsic,
                BarrierType.DownAndOut => s <= h ? k : intrinsic,
                _ => ThrowHelper.ThrowArgumentException<double>(ExceptionMessages.InvalidBarrierType),
            };
        }

        double vSqrtT = vol * Sqrt(tau);
        double expQt = Exp(-q * tau);
        double expRt = Exp(-r * tau);

        // Adjust barrier for discrete observations (Broadie, Glasserman, and Kou adjustment)
        if (dt > 0)
        {
            h = option.BarrierType switch
            {
                BarrierType.UpAndIn or BarrierType.UpAndOut => h * Exp(Beta * vol * Sqrt(dt)),
                BarrierType.DownAndIn or BarrierType.DownAndOut => h * Exp(-Beta * vol * Sqrt(dt)),
                _ => ThrowHelper.ThrowArgumentException<double>(ExceptionMessages.InvalidBarrierType),
            };
        }

        double mu = (r - q - 0.5 * vol * vol) / (vol * vol);
        double lambda = Sqrt(mu * mu + 2 * r / (vol * vol));
        double x1 = Log(s / x) / vSqrtT + (1 + mu) * vSqrtT;
        double x2 = Log(s / h) / vSqrtT + (1 + mu) * vSqrtT;
        double y1 = Log(h * h / (s * x)) / vSqrtT + (1 + mu) * vSqrtT;
        double y2 = Log(h / s) / vSqrtT + (1 + mu) * vSqrtT;
        double z = Log(h / s) / vSqrtT + lambda * vSqrtT;

        (double, double, double, double, double, double) CommonFactors(double eta, double phi)
        {
            double a = phi * s * expQt * N(phi * x1) - phi * x * expRt * N(phi * x1 - phi * vSqrtT);
            double b = phi * s * expQt * N(phi * x2) - phi * x * expRt * N(phi * x2 - phi * vSqrtT);
            double c = phi * s * expQt * Pow(h / s, 2 * (mu + 1)) * N(eta * y1) - phi * x * expRt * Pow(h / s, 2 * mu) * N(eta * y1 - eta * vSqrtT);
            double d = phi * s * expQt * Pow(h / s, 2 * (mu + 1)) * N(eta * y2) - phi * x * expRt * Pow(h / s, 2 * mu) * N(eta * y2 - eta * vSqrtT);
            double e = k * expRt * (N(eta * x2 - eta * vSqrtT) - Pow(h / s, 2 * mu) * N(eta * y2 - eta * vSqrtT));
            double f = option.RebatePaymentType == PaymentType.PayAtHit
                ? k * (Pow(h / s, mu + lambda) * N(eta * z) + Pow(h / s, mu - lambda) * N(eta * z - 2 * eta * lambda * vSqrtT))
                : k * expRt - e;

            return (a, b, c, d, e, f);
        }

        switch (option.OptionType)
        {
            case OptionType.Call:
            {
                switch (option.BarrierType)
                {
                    case BarrierType.UpAndIn:
                    {
                        (double a, double b, double c, double d, double e, double _) = CommonFactors(-1, 1);
                        return x > h ? a + e : b - c + d + e;
                    }

                    case BarrierType.DownAndIn:
                    {
                        (double a, double b, double c, double d, double e, double _) = CommonFactors(1, 1);
                        return x > h ? c + e : a - b + d + e;
                    }

                    case BarrierType.UpAndOut:
                    {
                        (double a, double b, double c, double d, double e, double f) = CommonFactors(-1, 1);
                        return x > h ? f : a - b + c - d + f;
                    }

                    case BarrierType.DownAndOut:
                    {
                        (double a, double b, double c, double d, double e, double f) = CommonFactors(1, 1);
                        return x > h ? a - c + f : b - d + f;
                    }

                    default:
                        return ThrowHelper.ThrowArgumentException<double>(ExceptionMessages.InvalidBarrierType);
                }
            }

            case OptionType.Put:
            {
                switch (option.BarrierType)
                {
                    case BarrierType.UpAndIn:
                    {
                        (double a, double b, double c, double d, double e, double _) = CommonFactors(-1, -1);
                        return x > h ? a - b + d + e : c + e;
                    }

                    case BarrierType.DownAndIn:
                    {
                        (double a, double b, double c, double d, double e, double _) = CommonFactors(1, -1);
                        return x > h ? b - c + d + e : a + e;
                    }

                    case BarrierType.UpAndOut:
                    {
                        (double a, double b, double c, double d, double e, double f) = CommonFactors(-1, -1);
                        return x > h ? b - d + f : a - c + f;
                    }

                    case BarrierType.DownAndOut:
                    {
                        (double a, double b, double c, double d, double e, double f) = CommonFactors(1, -1);
                        return x > h ? a - b + c - d + f : f;
                    }

                    default:
                        return ThrowHelper.ThrowArgumentException<double>(ExceptionMessages.InvalidBarrierType);
                }
            }

            default:
                return ThrowHelper.ThrowArgumentException<double>(ExceptionMessages.InvalidOptionType);
        }
    }

    private static double N(double x) => Normal.CDF(0, 1, x);
}
