using System.Diagnostics;
using CommunityToolkit.Diagnostics;
using DerivaSharp.Instruments;

namespace DerivaSharp.PricingEngines;

public sealed class FdSingleTouchEngine(FiniteDifferenceScheme scheme, int priceStepCount, int timeStepCount)
    : FiniteDifferencePricingEngine<SingleTouchOption>(scheme, priceStepCount, timeStepCount)
{
    protected override double CalculateValue(SingleTouchOption option, PricingContext context)
    {
        if (option.TouchType is TouchType.NoTouchUp or TouchType.NoTouchDown)
        {
            Guard.IsTrue(option.RebatePaymentType == PaymentType.PayAtExpiry);

            TouchType otType = option.TouchType == TouchType.NoTouchUp ? TouchType.OneTouchUp : TouchType.OneTouchDown;
            SingleTouchOption otOption = option with { TouchType = otType };
            double otValue = base.CalculateValue(otOption, context);

            double r = context.RiskFreeRate;
            double tau = GetYearsToExpiration(option, context);
            double dfr = Math.Exp(-r * tau);

            return option.Rebate * dfr - otValue;
        }

        return base.CalculateValue(option, context);
    }

    protected override void InitializeCoefficients(SingleTouchOption option, PricingContext context)
    {
        if (option.TouchType is TouchType.NoTouchUp or TouchType.NoTouchDown)
        {
            ThrowHelper.ThrowInvalidOperationException("Use cash parity for no-touch options.");
        }

        switch (option.TouchType)
        {
            case TouchType.OneTouchUp:
                MinPrice = 0;
                MaxPrice = option.BarrierPrice;
                break;
            case TouchType.OneTouchDown:
                MinPrice = option.BarrierPrice;
                MaxPrice = 4 * Math.Max(option.StrikePrice, option.BarrierPrice);
                break;
            case TouchType.NoTouchUp:
            case TouchType.NoTouchDown:
            default:
                Debug.Assert(false);
                break;
        }

        base.InitializeCoefficients(option, context);
    }

    protected override void SetTerminalCondition(SingleTouchOption option)
    {
    }

    protected override void SetBoundaryConditions(SingleTouchOption option, PricingContext context)
    {
        double k = option.Rebate;
        double r = context.RiskFreeRate;

        double maxTime = TimeVector[^1];

        for (int i = 0; i < TimeVector.Length - 1; i++)
        {
            double tau = maxTime - TimeVector[i];
            double dfr = Math.Exp(-r * tau);

            switch (option.TouchType)
            {
                case TouchType.OneTouchUp:
                    ValueMatrixSpan[i, ^1] = option.RebatePaymentType == PaymentType.PayAtHit ? k : k * dfr;
                    break;
                case TouchType.OneTouchDown:
                    ValueMatrixSpan[i, 0] = option.RebatePaymentType == PaymentType.PayAtHit ? k : k * dfr;
                    break;
                default:
                    Debug.Assert(false);
                    break;
            }
        }
    }

    protected override void ApplyStepConditions(int i, SingleTouchOption option, PricingContext context)
    {
    }
}
