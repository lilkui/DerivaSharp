using CommunityToolkit.Diagnostics;
using DerivaSharp.Instruments;

namespace DerivaSharp.PricingEngines;

public sealed class FdEuropeanEngine(FiniteDifferenceScheme scheme, int priceStepCount, int timeStepCount)
    : FiniteDifferencePricingEngine<EuropeanOption>(scheme, priceStepCount, timeStepCount)
{
    protected override double CalculateValue(EuropeanOption option, PricingContext context)
    {
        if (context.ValuationDate == option.ExpirationDate)
        {
            double z = (int)option.OptionType;
            return Math.Max(z * (context.AssetPrice - option.StrikePrice), 0);
        }

        return base.CalculateValue(option, context);
    }

    protected override void InitializeCoefficients(EuropeanOption option, PricingContext context)
    {
        MinPrice = 0;
        MaxPrice = 4 * option.StrikePrice;
        base.InitializeCoefficients(option, context);
    }

    protected override void SetTerminalCondition(EuropeanOption option)
    {
        double x = option.StrikePrice;
        int z = (int)option.OptionType;

        for (int j = 0; j < PriceVector.Length; j++)
        {
            ValueMatrixSpan[^1, j] = Math.Max(z * (PriceVector[j] - x), 0);
        }
    }

    protected override void SetBoundaryConditions(EuropeanOption option, PricingContext context)
    {
        double x = option.StrikePrice;
        double r = context.RiskFreeRate;
        double q = context.DividendYield;

        double maxTime = TimeVector[^1];
        double minPrice = PriceVector[0];
        double maxPrice = PriceVector[^1];

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
                    ThrowHelper.ThrowArgumentException("Invalid option type.");
                    break;
            }
        }
    }

    protected override void ApplyStepConditions(int i, EuropeanOption option, PricingContext context)
    {
    }
}
