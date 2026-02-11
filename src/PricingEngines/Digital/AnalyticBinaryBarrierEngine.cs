using CommunityToolkit.Diagnostics;
using DerivaSharp.Instruments;
using DerivaSharp.Models;
using MathNet.Numerics.Distributions;
using static System.Math;

namespace DerivaSharp.PricingEngines;

/// <summary>
///     Pricing engine for binary barrier options using closed-form analytical formulas with discrete barrier adjustment.
/// </summary>
public sealed class AnalyticBinaryBarrierEngine : BsmPricingEngine<BinaryBarrierOption>
{
    private const double Beta = 0.5825971579390107; // Correction factor for discrete barriers

    protected override double CalculateValue(BinaryBarrierOption option, BsmModelParameters parameters, double assetPrice, DateOnly valuationDate)
    {
        double x = option.StrikePrice;
        double h = option.BarrierPrice;
        double k = option.Rebate;
        double dt = option.ObservationInterval;
        double s = assetPrice;
        double tau = GetYearsToExpiration(option, valuationDate);
        double vol = parameters.Volatility;
        double r = parameters.RiskFreeRate;
        double q = parameters.DividendYield;

        if (tau <= 0)
        {
            return CalculateTerminalPayoff(option, s);
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

        switch (option)
        {
            case CashOrNothingBarrierOption con:
                switch (con)
                {
                    // [1] Down-and-in cash-(at-hit)-or-nothing (S>H)
                    case { BarrierType: BarrierType.DownAndIn, RebatePaymentType: PaymentType.PayAtHit }:
                    {
                        Guard.IsGreaterThan(s, h);
                        Factors factors = CommonFactors(1, 0);
                        return factors.A5;
                    }

                    // [2] Up-and-in cash-(at-hit)-or-nothing (S<H)
                    case { BarrierType: BarrierType.UpAndIn, RebatePaymentType: PaymentType.PayAtHit }:
                    {
                        Guard.IsLessThan(s, h);
                        Factors factors = CommonFactors(-1, 0);
                        return factors.A5;
                    }

                    // [5] Down-and-in cash-(at-expiry)-or-nothing (S>H)
                    case { BarrierType: BarrierType.DownAndIn, RebatePaymentType: PaymentType.PayAtExpiry, OptionType: null }:
                    {
                        Guard.IsGreaterThan(s, h);
                        Factors factors = CommonFactors(1, -1);
                        return factors.B2 + factors.B4;
                    }

                    // [6] Up-and-in cash-(at-expiry)-or-nothing (S<H)
                    case { BarrierType: BarrierType.UpAndIn, RebatePaymentType: PaymentType.PayAtExpiry, OptionType: null }:
                    {
                        Guard.IsLessThan(s, h);
                        Factors factors = CommonFactors(-1, 1);
                        return factors.B2 + factors.B4;
                    }

                    // [9] Down-and-out cash-(at-expiry)-or-nothing (S>H)
                    case { BarrierType: BarrierType.DownAndOut, RebatePaymentType: PaymentType.PayAtExpiry, OptionType: null }:
                    {
                        Guard.IsGreaterThan(s, h);
                        Factors factors = CommonFactors(1, 1);
                        return factors.B2 - factors.B4;
                    }

                    // [10] Up-and-out cash-(at-expiry)-or-nothing (S<H)
                    case { BarrierType: BarrierType.UpAndOut, RebatePaymentType: PaymentType.PayAtExpiry, OptionType: null }:
                    {
                        Guard.IsLessThan(s, h);
                        Factors factors = CommonFactors(-1, -1);
                        return factors.B2 - factors.B4;
                    }

                    // [13] Down-and-in cash-(at-expiry)-or-nothing call (S>H)
                    case { BarrierType: BarrierType.DownAndIn, RebatePaymentType: PaymentType.PayAtExpiry, OptionType: OptionType.Call }:
                    {
                        Guard.IsGreaterThan(s, h);
                        Factors factors = CommonFactors(1, 1);
                        return x > h ? factors.B3 : factors.B1 - factors.B2 + factors.B4;
                    }

                    // [14] Up-and-in cash-(at-expiry)-or-nothing call (S<H)
                    case { BarrierType: BarrierType.UpAndIn, RebatePaymentType: PaymentType.PayAtExpiry, OptionType: OptionType.Call }:
                    {
                        Guard.IsLessThan(s, h);
                        Factors factors = CommonFactors(-1, 1);
                        return x > h ? factors.B1 : factors.B2 - factors.B3 + factors.B4;
                    }

                    // [17] Down-and-in cash-(at-expiry)-or-nothing put (S>H)
                    case { BarrierType: BarrierType.DownAndIn, RebatePaymentType: PaymentType.PayAtExpiry, OptionType: OptionType.Put }:
                    {
                        Guard.IsGreaterThan(s, h);
                        Factors factors = CommonFactors(1, -1);
                        return x > h ? factors.B2 - factors.B3 + factors.B4 : factors.B1;
                    }

                    // [18] Up-and-in cash-(at-expiry)-or-nothing put (S<H)
                    case { BarrierType: BarrierType.UpAndIn, RebatePaymentType: PaymentType.PayAtExpiry, OptionType: OptionType.Put }:
                    {
                        Guard.IsLessThan(s, h);
                        Factors factors = CommonFactors(-1, -1);
                        return x > h ? factors.B1 - factors.B2 + factors.B4 : factors.B3;
                    }

                    // [21] Down-and-out cash-(at-expiry)-or-nothing call (S>H)
                    case { BarrierType: BarrierType.DownAndOut, RebatePaymentType: PaymentType.PayAtExpiry, OptionType: OptionType.Call }:
                    {
                        Guard.IsGreaterThan(s, h);
                        Factors factors = CommonFactors(1, 1);
                        return x > h ? factors.B1 - factors.B3 : factors.B2 - factors.B4;
                    }

                    // [22] Up-and-out cash-(at-expiry)-or-nothing call (S<H)
                    case { BarrierType: BarrierType.UpAndOut, RebatePaymentType: PaymentType.PayAtExpiry, OptionType: OptionType.Call }:
                    {
                        Guard.IsLessThan(s, h);
                        Factors factors = CommonFactors(-1, 1);
                        return x > h ? 0 : factors.B1 - factors.B2 + factors.B3 - factors.B4;
                    }

                    // [25] Down-and-out cash-(at-expiry)-or-nothing put (S>H)
                    case { BarrierType: BarrierType.DownAndOut, RebatePaymentType: PaymentType.PayAtExpiry, OptionType: OptionType.Put }:
                    {
                        Guard.IsGreaterThan(s, h);
                        Factors factors = CommonFactors(1, -1);
                        return x > h ? factors.B1 - factors.B2 + factors.B3 - factors.B4 : 0;
                    }

                    // [26] Up-and-out cash-(at-expiry)-or-nothing put (S<H)
                    case { BarrierType: BarrierType.UpAndOut, OptionType: OptionType.Put }:
                    {
                        Guard.IsLessThan(s, h);
                        Factors factors = CommonFactors(-1, -1);
                        return x > h ? factors.B2 - factors.B4 : factors.B1 - factors.B3;
                    }
                }

                break;

            case AssetOrNothingBarrierOption aon:
                switch (aon)
                {
                    // [3] Down-and-in asset-(at-hit)-or-nothing (K=H) (S>H)
                    case { BarrierType: BarrierType.DownAndIn, RebatePaymentType: PaymentType.PayAtHit }:
                    {
                        Guard.IsGreaterThan(s, h);
                        Guard.IsEqualTo(k, h);
                        Factors factors = CommonFactors(1, 0);
                        return factors.A5;
                    }

                    // [4] Up-and-in asset-(at-hit)-or-nothing (K=H) (S<H)
                    case { BarrierType: BarrierType.UpAndIn, RebatePaymentType: PaymentType.PayAtHit }:
                    {
                        Guard.IsLessThan(s, h);
                        Guard.IsEqualTo(k, h);
                        Factors factors = CommonFactors(-1, 0);
                        return factors.A5;
                    }

                    // [7] Down-and-in asset-(at-expiry)-or-nothing (S>H)
                    case { BarrierType: BarrierType.DownAndIn, RebatePaymentType: PaymentType.PayAtExpiry, OptionType: null }:
                    {
                        Guard.IsGreaterThan(s, h);
                        Factors factors = CommonFactors(1, -1);
                        return factors.A2 + factors.A4;
                    }

                    // [8] Up-and-in asset-(at-expiry)-or-nothing (S<H)
                    case { BarrierType: BarrierType.UpAndIn, RebatePaymentType: PaymentType.PayAtExpiry, OptionType: null }:
                    {
                        Guard.IsLessThan(s, h);
                        Factors factors = CommonFactors(-1, 1);
                        return factors.A2 + factors.A4;
                    }

                    // [11] Down-and-out asset-(at-expiry)-or-nothing (S>H)
                    case { BarrierType: BarrierType.DownAndOut, RebatePaymentType: PaymentType.PayAtExpiry, OptionType: null }:
                    {
                        Guard.IsGreaterThan(s, h);
                        Factors factors = CommonFactors(1, 1);
                        return factors.A2 - factors.A4;
                    }

                    // [12] Up-and-out asset-(at-expiry)-or-nothing (S<H)
                    case { BarrierType: BarrierType.UpAndOut, RebatePaymentType: PaymentType.PayAtExpiry, OptionType: null }:
                    {
                        Guard.IsLessThan(s, h);
                        Factors factors = CommonFactors(-1, -1);
                        return factors.A2 - factors.A4;
                    }

                    // [15] Down-and-in asset-(at-expiry)-or-nothing call (S>H)
                    case { BarrierType: BarrierType.DownAndIn, RebatePaymentType: PaymentType.PayAtExpiry, OptionType: OptionType.Call }:
                    {
                        Guard.IsGreaterThan(s, h);
                        Factors factors = CommonFactors(1, 1);
                        return x > h ? factors.A3 : factors.A1 - factors.A2 + factors.A4;
                    }

                    // [16] Up-and-in asset-(at-expiry)-or-nothing call (S<H)
                    case { BarrierType: BarrierType.UpAndIn, RebatePaymentType: PaymentType.PayAtExpiry, OptionType: OptionType.Call }:
                    {
                        Guard.IsLessThan(s, h);
                        Factors factors = CommonFactors(-1, 1);
                        return x > h ? factors.A1 : factors.A2 - factors.A3 + factors.A4;
                    }

                    // [19] Down-and-in asset-(at-expiry)-or-nothing put (S>H)
                    case { BarrierType: BarrierType.DownAndIn, RebatePaymentType: PaymentType.PayAtExpiry, OptionType: OptionType.Put }:
                    {
                        Guard.IsGreaterThan(s, h);
                        Factors factors = CommonFactors(1, -1);
                        return x > h ? factors.A2 - factors.A3 + factors.A4 : factors.A1;
                    }

                    // [20] Up-and-in asset-(at-expiry)-or-nothing put (S<H)
                    case { BarrierType: BarrierType.UpAndIn, RebatePaymentType: PaymentType.PayAtExpiry, OptionType: OptionType.Put }:
                    {
                        Guard.IsLessThan(s, h);
                        Factors factors = CommonFactors(-1, -1);
                        return x > h ? factors.A1 - factors.A2 + factors.A3 : factors.A3;
                    }

                    // [23] Down-and-out asset-(at-expiry)-or-nothing call (S>H)
                    case { BarrierType: BarrierType.DownAndOut, RebatePaymentType: PaymentType.PayAtExpiry, OptionType: OptionType.Call }:
                    {
                        Guard.IsGreaterThan(s, h);
                        Factors factors = CommonFactors(1, 1);
                        return x > h ? factors.A1 - factors.A3 : factors.A2 - factors.A4;
                    }

                    // [24] Up-and-out asset-(at-expiry)-or-nothing call (S<H)
                    case { BarrierType: BarrierType.UpAndOut, RebatePaymentType: PaymentType.PayAtExpiry, OptionType: OptionType.Call }:
                    {
                        Guard.IsLessThan(s, h);
                        Factors factors = CommonFactors(-1, 1);
                        return x > h ? 0 : factors.A1 - factors.A2 + factors.A3 - factors.A4;
                    }

                    // [27] Down-and-out asset-(at-expiry)-or-nothing put (S>H)
                    case { BarrierType: BarrierType.DownAndOut, RebatePaymentType: PaymentType.PayAtExpiry, OptionType: OptionType.Put }:
                    {
                        Guard.IsGreaterThan(s, h);
                        Factors factors = CommonFactors(1, -1);
                        return x > h ? factors.A1 - factors.A2 + factors.A3 - factors.A4 : 0;
                    }

                    // [28] Up-and-out asset-(at-expiry)-or-nothing put (S<H)
                    case { BarrierType: BarrierType.UpAndOut, RebatePaymentType: PaymentType.PayAtExpiry, OptionType: OptionType.Put }:
                    {
                        Guard.IsLessThan(s, h);
                        Factors factors = CommonFactors(-1, -1);
                        return x > h ? factors.A2 - factors.A4 : factors.A1 - factors.A3;
                    }
                }

                break;
        }

        return ThrowHelper.ThrowArgumentException<double>(ExceptionMessages.InvalidBinaryBarrierOption);

        Factors CommonFactors(double eta, double phi)
        {
            double a1 = s * expQt * N(phi * x1);
            double b1 = k * expRt * N(phi * x1 - phi * vSqrtT);
            double a2 = s * expQt * N(phi * x2);
            double b2 = k * expRt * N(phi * x2 - phi * vSqrtT);
            double a3 = s * expQt * Pow(h / s, 2 * (mu + 1)) * N(eta * y1);
            double b3 = k * expRt * Pow(h / s, 2 * mu) * N(eta * y1 - eta * vSqrtT);
            double a4 = s * expQt * Pow(h / s, 2 * (mu + 1)) * N(eta * y2);
            double b4 = k * expRt * Pow(h / s, 2 * mu) * N(eta * y2 - eta * vSqrtT);
            double a5 = k * (Pow(h / s, mu + lambda) * N(eta * z) + Pow(h / s, mu - lambda) * N(eta * z - 2 * eta * lambda * vSqrtT));

            return new Factors(a1, b1, a2, b2, a3, b3, a4, b4, a5);
        }
    }

    private static double CalculateTerminalPayoff(BinaryBarrierOption option, double assetPrice)
    {
        double x = option.StrikePrice;
        double h = option.BarrierPrice;
        double k = option.Rebate;
        double s = assetPrice;

        switch (option)
        {
            case CashOrNothingBarrierOption con:
            {
                bool inTheMoney = con.OptionType switch
                {
                    OptionType.Call => s > x,
                    OptionType.Put => s < x,
                    null => true,
                    _ => ThrowHelper.ThrowArgumentException<bool>(ExceptionMessages.InvalidOptionType),
                };

                bool hit = con.BarrierType switch
                {
                    BarrierType.UpAndIn or BarrierType.UpAndOut => s >= h,
                    BarrierType.DownAndIn or BarrierType.DownAndOut => s <= h,
                    _ => ThrowHelper.ThrowArgumentException<bool>(ExceptionMessages.InvalidBarrierType),
                };

                return con.BarrierType switch
                {
                    BarrierType.UpAndIn or BarrierType.DownAndIn => hit && inTheMoney ? k : 0,
                    BarrierType.UpAndOut or BarrierType.DownAndOut => hit ? 0 : inTheMoney ? k : 0,
                    _ => ThrowHelper.ThrowArgumentException<double>(ExceptionMessages.InvalidBarrierType),
                };
            }

            case AssetOrNothingBarrierOption aon:
            {
                bool inTheMoney = aon.OptionType switch
                {
                    OptionType.Call => s > x,
                    OptionType.Put => s < x,
                    null => true,
                    _ => ThrowHelper.ThrowArgumentException<bool>(ExceptionMessages.InvalidOptionType),
                };

                bool hit = aon.BarrierType switch
                {
                    BarrierType.UpAndIn or BarrierType.UpAndOut => s >= h,
                    BarrierType.DownAndIn or BarrierType.DownAndOut => s <= h,
                    _ => ThrowHelper.ThrowArgumentException<bool>(ExceptionMessages.InvalidBarrierType),
                };

                return aon.BarrierType switch
                {
                    BarrierType.UpAndIn or BarrierType.DownAndIn => hit && inTheMoney ? s : 0,
                    BarrierType.UpAndOut or BarrierType.DownAndOut => hit ? 0 : inTheMoney ? s : 0,
                    _ => ThrowHelper.ThrowArgumentException<double>(ExceptionMessages.InvalidBarrierType),
                };
            }

            default:
                return ThrowHelper.ThrowArgumentException<double>(ExceptionMessages.InvalidBinaryBarrierOption);
        }
    }

    private static double N(double x) => Normal.CDF(0, 1, x);

    private record struct Factors(double A1, double B1, double A2, double B2, double A3, double B3, double A4, double B4, double A5);
}
