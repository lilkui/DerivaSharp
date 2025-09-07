using System.Diagnostics;
using CommunityToolkit.Diagnostics;
using DerivaSharp.Instruments;
using MathNet.Numerics.Distributions;

namespace DerivaSharp.PricingEngines;

public sealed class FdBarrierEngine(FiniteDifferenceScheme scheme, int priceStepCount, int timeStepCount)
    : FiniteDifferencePricingEngine<BarrierOption>(scheme, priceStepCount, timeStepCount)
{
    private bool[]? _isObservationTime;

    protected override double CalculateValue(BarrierOption option, PricingContext context)
    {
        if (context.ValuationDate == option.ExpirationDate)
        {
            double x = option.StrikePrice;
            double h = option.BarrierPrice;
            double k = option.Rebate;
            double s = context.AssetPrice;
            int z = (int)option.OptionType;
            double intrinsic = Math.Max(z * (s - x), 0);

            return option.BarrierType switch
            {
                BarrierType.UpAndIn => s >= h ? intrinsic : k,
                BarrierType.DownAndIn => s <= h ? intrinsic : k,
                BarrierType.UpAndOut => s >= h ? k : intrinsic,
                BarrierType.DownAndOut => s <= h ? k : intrinsic,
                _ => ThrowHelper.ThrowArgumentException<double>(ExceptionMessages.InvalidBarrierType),
            };
        }

        return base.CalculateValue(option, context);
    }

    protected override void InitializeCoefficients(BarrierOption option, PricingContext context)
    {
        if (option.ObservationInterval == 0) // continuous observation
        {
            switch (option.BarrierType)
            {
                case BarrierType.UpAndOut:
                case BarrierType.UpAndIn:
                    MinPrice = 0;
                    MaxPrice = option.BarrierPrice;
                    break;

                case BarrierType.DownAndOut:
                case BarrierType.DownAndIn:
                    MinPrice = option.BarrierPrice;
                    MaxPrice = 4 * Math.Max(option.StrikePrice, option.BarrierPrice);
                    break;

                default:
                    Debug.Fail(ExceptionMessages.InvalidBarrierType);
                    break;
            }
        }
        else // discrete observation
        {
            MinPrice = 0;
            MaxPrice = 4 * Math.Max(option.StrikePrice, option.BarrierPrice);
        }

        base.InitializeCoefficients(option, context);

        if (option.ObservationInterval > 0)
        {
            BuildObservationSchedule(option);
        }
        else
        {
            _isObservationTime = null;
        }
    }

    protected override void SetTerminalCondition(BarrierOption option)
    {
        double x = option.StrikePrice;
        double k = option.Rebate;
        int z = (int)option.OptionType;

        if (option.BarrierType is BarrierType.UpAndOut or BarrierType.DownAndOut)
        {
            for (int j = 0; j < PriceVector.Length; j++)
            {
                ValueMatrixSpan[^1, j] = Math.Max(z * (PriceVector[j] - x), 0);
            }
        }
        else // knock-in
        {
            for (int j = 0; j < PriceVector.Length; j++)
            {
                ValueMatrixSpan[^1, j] = k;
            }
        }
    }

    protected override void SetBoundaryConditions(BarrierOption option, PricingContext context)
    {
        double x = option.StrikePrice;
        double k = option.Rebate;
        double z = (int)option.OptionType;
        double r = context.RiskFreeRate;
        double q = context.DividendYield;

        double maxTime = TimeVector[^1];
        double minPrice = PriceVector[0];
        double maxPrice = PriceVector[^1];

        if (option.ObservationInterval == 0) // continuous observation
        {
            for (int i = 0; i < TimeVector.Length; i++)
            {
                double tau = maxTime - TimeVector[i];
                double dfq = Math.Exp(-q * tau);
                double dfr = Math.Exp(-r * tau);
                double pvRebate = option.RebatePaymentType == PaymentType.PayAtHit ? k : k * dfr;

                switch (option.BarrierType)
                {
                    case BarrierType.UpAndOut:
                        ValueMatrixSpan[i, 0] = Math.Max(z * (minPrice * dfq - x * dfr), 0);
                        ValueMatrixSpan[i, ^1] = pvRebate;
                        break;
                    case BarrierType.DownAndOut:
                        ValueMatrixSpan[i, 0] = pvRebate;
                        ValueMatrixSpan[i, ^1] = Math.Max(z * (maxPrice * dfq - x * dfr), 0);
                        break;
                    case BarrierType.UpAndIn:
                        ValueMatrixSpan[i, 0] = Math.Max(z * (minPrice * dfq - x * dfr), 0);
                        ValueMatrixSpan[i, ^1] = EuropeanValue(option, context with { AssetPrice = maxPrice }, tau);
                        break;
                    case BarrierType.DownAndIn:
                        ValueMatrixSpan[i, 0] = EuropeanValue(option, context with { AssetPrice = minPrice }, tau);
                        ValueMatrixSpan[i, ^1] = Math.Max(z * (maxPrice * dfq - x * dfr), 0);
                        break;
                    default:
                        Debug.Fail(ExceptionMessages.InvalidBarrierType);
                        break;
                }
            }
        }
        else // discrete observation
        {
            for (int i = 0; i < TimeVector.Length - 1; i++)
            {
                double tau = maxTime - TimeVector[i];
                double dfq = Math.Exp(-q * tau);
                double dfr = Math.Exp(-r * tau);

                switch (option.OptionType)
                {
                    case OptionType.Call:
                        ValueMatrixSpan[i, 0] = 0.0;
                        ValueMatrixSpan[i, ^1] = maxPrice * dfq - x * dfr;
                        break;
                    case OptionType.Put:
                        ValueMatrixSpan[i, 0] = x * dfr - minPrice * dfq;
                        ValueMatrixSpan[i, ^1] = 0.0;
                        break;
                    default:
                        ThrowHelper.ThrowArgumentException(ExceptionMessages.InvalidOptionType);
                        break;
                }
            }
        }
    }

    protected override void ApplyStepConditions(int i, BarrierOption option, PricingContext context)
    {
        // Continuous observation: nothing to do (already handled by boundary conditions)
        if (option.ObservationInterval == 0)
        {
            return;
        }

        Debug.Assert(_isObservationTime is not null);
        if (!_isObservationTime[i])
        {
            return;
        }

        Span<double> currentValues = ValueMatrixSpan.GetRowSpan(i);
        double barrier = option.BarrierPrice;
        double k = option.Rebate;
        double tau = TimeVector[^1] - TimeVector[i];
        double pvRebate = option.RebatePaymentType == PaymentType.PayAtHit ? k : k * Math.Exp(-context.RiskFreeRate * tau);

        switch (option.BarrierType)
        {
            case BarrierType.UpAndOut:
                for (int j = 0; j < PriceVector.Length; j++)
                {
                    if (PriceVector[j] >= barrier)
                    {
                        currentValues[j] = pvRebate;
                    }
                }

                break;

            case BarrierType.DownAndOut:
                for (int j = 0; j < PriceVector.Length; j++)
                {
                    if (PriceVector[j] <= barrier)
                    {
                        currentValues[j] = pvRebate;
                    }
                }

                break;

            case BarrierType.UpAndIn:
                for (int j = 0; j < PriceVector.Length; j++)
                {
                    if (PriceVector[j] >= barrier)
                    {
                        currentValues[j] = EuropeanValue(option, context with { AssetPrice = PriceVector[j] }, tau);
                    }
                }

                break;

            case BarrierType.DownAndIn:
                for (int j = 0; j < PriceVector.Length; j++)
                {
                    if (PriceVector[j] <= barrier)
                    {
                        currentValues[j] = EuropeanValue(option, context with { AssetPrice = PriceVector[j] }, tau);
                    }
                }

                break;

            default:
                Debug.Fail(ExceptionMessages.InvalidBarrierType);
                break;
        }
    }

    private static double EuropeanValue(BarrierOption option, PricingContext context, double tau)
    {
        double x = option.StrikePrice;
        int z = (int)option.OptionType;
        double s = context.AssetPrice;
        double vol = context.Volatility;
        double r = context.RiskFreeRate;
        double q = context.DividendYield;

        if (tau == 0)
        {
            return Math.Max(z * (s - x), 0);
        }

        double d1 = (Math.Log(s / x) + (r - q + vol * vol / 2) * tau) / (vol * Math.Sqrt(tau));
        double d2 = d1 - vol * Math.Sqrt(tau);

        return z * (s * Math.Exp(-q * tau) * StdNormCdf(z * d1) - x * Math.Exp(-r * tau) * StdNormCdf(z * d2));

        static double StdNormCdf(double x)
        {
            return Normal.CDF(0, 1, x);
        }
    }

    private void BuildObservationSchedule(BarrierOption option)
    {
        double interval = option.ObservationInterval;
        Debug.Assert(interval > 0);

        int nTimes = TimeVector.Length;
        Debug.Assert(nTimes >= 2);

        _isObservationTime = new bool[nTimes];

        double tau = TimeVector[^1];
        double dt = TimeVector[1] - TimeVector[0];

        int maxObs = (int)Math.Floor((tau + 1e-12) / interval);
        for (int obs = 1; obs <= maxObs; obs++)
        {
            int gridIndex = (int)Math.Round(obs * interval / dt);
            gridIndex = Math.Clamp(gridIndex, 0, nTimes - 1);
            _isObservationTime[gridIndex] = true;
        }

        _isObservationTime[^1] = true;
    }
}
