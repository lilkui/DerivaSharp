using System.Diagnostics;
using CommunityToolkit.Diagnostics;
using DerivaSharp.Instruments;

namespace DerivaSharp.PricingEngines;

public sealed class FdBarrierEngine(FiniteDifferenceScheme scheme, int priceStepCount, int timeStepCount)
    : FiniteDifferencePricingEngine<BarrierOption>(scheme, priceStepCount, timeStepCount)
{
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

        if (option.BarrierType is BarrierType.UpAndIn or BarrierType.DownAndIn)
        {
            EuropeanOption eurOption = new(option.OptionType, option.StrikePrice, option.EffectiveDate, option.ExpirationDate);
            AnalyticEuropeanEngine eurEngine = new();
            double eurValue = eurEngine.Value(eurOption, context);

            BarrierType koType = option.BarrierType == BarrierType.UpAndIn ? BarrierType.UpAndOut : BarrierType.DownAndOut;
            BarrierOption koOption = option with { BarrierType = koType };
            double koValue = base.CalculateValue(koOption, context);

            double r = context.RiskFreeRate;
            double tau = GetYearsToExpiration(option, context);
            double pvRebate = option.Rebate * Math.Exp(-r * tau);

            Debug.Assert(option.RebatePaymentType == PaymentType.PayAtExpiry);

            return eurValue + pvRebate - koValue;
        }

        return base.CalculateValue(option, context);
    }

    protected override void InitializeCoefficients(BarrierOption option, PricingContext context)
    {
        if (option.BarrierType is BarrierType.UpAndIn or BarrierType.DownAndIn)
        {
            ThrowHelper.ThrowInvalidOperationException(ExceptionMessages.UseInOutParityForKnockIn);
        }

        if (option.ObservationInterval == 0) // continuous observation
        {
            switch (option.BarrierType)
            {
                case BarrierType.UpAndOut:
                    MinPrice = 0;
                    MaxPrice = option.BarrierPrice;
                    break;
                case BarrierType.DownAndOut:
                    MinPrice = option.BarrierPrice;
                    MaxPrice = 4 * Math.Max(option.StrikePrice, option.BarrierPrice);
                    break;
                case BarrierType.UpAndIn:
                case BarrierType.DownAndIn:
                default:
                    Debug.Assert(false);
                    break;
            }
        }
        else // discrete observation
        {
            MinPrice = 0;
            MaxPrice = 4 * Math.Max(option.StrikePrice, option.BarrierPrice);
        }

        base.InitializeCoefficients(option, context);
    }

    protected override void SetTerminalCondition(BarrierOption option)
    {
        double x = option.StrikePrice;
        int z = (int)option.OptionType;

        for (int j = 0; j < PriceVector.Length; j++)
        {
            ValueMatrixSpan[^1, j] = Math.Max(z * (PriceVector[j] - x), 0);
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

                switch (option.BarrierType)
                {
                    case BarrierType.UpAndOut:
                        ValueMatrixSpan[i, 0] = Math.Max(z * (minPrice * dfq - x * dfr), 0);
                        ValueMatrixSpan[i, ^1] = option.RebatePaymentType == PaymentType.PayAtHit ? k : k * dfr;
                        break;
                    case BarrierType.DownAndOut:
                        ValueMatrixSpan[i, 0] = option.RebatePaymentType == PaymentType.PayAtHit ? k : k * dfr;
                        ValueMatrixSpan[i, ^1] = Math.Max(z * (maxPrice * dfq - x * dfr), 0);
                        break;
                    default:
                        Debug.Assert(false);
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

    // Note: For simplicity, this implementation assumes every time step is an observation date.
    protected override void ApplyStepConditions(int i, BarrierOption option, PricingContext context)
    {
        if (option.ObservationInterval == 0)
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

            default:
                Debug.Assert(false);
                break;
        }
    }
}
