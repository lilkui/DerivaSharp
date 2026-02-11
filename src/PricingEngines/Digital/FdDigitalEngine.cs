using CommunityToolkit.Diagnostics;
using DerivaSharp.Instruments;
using DerivaSharp.Models;

namespace DerivaSharp.PricingEngines;

/// <summary>
///     Pricing engine for digital options using finite difference methods.
/// </summary>
/// <param name="scheme">The finite difference scheme to use.</param>
/// <param name="priceStepCount">The number of price steps in the grid.</param>
/// <param name="timeStepCount">The number of time steps in the grid.</param>
public sealed class FdDigitalEngine(FiniteDifferenceScheme scheme, int priceStepCount, int timeStepCount)
    : BsmFiniteDifferenceEngine<DigitalOption>(scheme, priceStepCount, timeStepCount)
{
    protected override double CalculateValue(DigitalOption option, BsmModelParameters parameters, double assetPrice, DateOnly valuationDate)
    {
        if (valuationDate == option.ExpirationDate)
        {
            double x = option.StrikePrice;
            bool inTheMoney = option.OptionType == OptionType.Call ? assetPrice > x : assetPrice < x;

            return option switch
            {
                CashOrNothingOption con => inTheMoney ? con.Rebate : 0.0,
                AssetOrNothingOption _ => inTheMoney ? assetPrice : 0.0,
                _ => ThrowHelper.ThrowArgumentException<double>(ExceptionMessages.InvalidDigitalOption),
            };
        }

        return base.CalculateValue(option, parameters, assetPrice, valuationDate);
    }

    protected override void InitializeGrid(DigitalOption option, BsmModelParameters parameters, DateOnly valuationDate)
    {
        MinPrice = 0;
        double strike = option.StrikePrice;
        double maxPrice = 4 * strike;

        double rawIndex = PriceStepCount * strike / maxPrice;
        int strikeIndex = Math.Max(1, (int)Math.Round(rawIndex));
        MaxPrice = strike * PriceStepCount / strikeIndex;

        base.InitializeGrid(option, parameters, valuationDate);
    }

    protected override void SetTerminalCondition(DigitalOption option)
    {
        double x = option.StrikePrice;
        double epsilon = 1e-12 * Math.Max(1.0, x);

        for (int j = 0; j < PriceVector.Length; j++)
        {
            double s = PriceVector[j];
            bool isAtTheMoney = Math.Abs(s - x) <= epsilon;
            if (isAtTheMoney)
            {
                ValueMatrixSpan[^1, j] = option switch
                {
                    CashOrNothingOption con => 0.5 * con.Rebate,
                    AssetOrNothingOption _ => 0.5 * s,
                    _ => ThrowHelper.ThrowArgumentException<double>(ExceptionMessages.InvalidDigitalOption),
                };
                continue;
            }

            bool inTheMoney = option.OptionType == OptionType.Call ? s > x : s < x;

            ValueMatrixSpan[^1, j] = option switch
            {
                CashOrNothingOption con => inTheMoney ? con.Rebate : 0.0,
                AssetOrNothingOption _ => inTheMoney ? s : 0.0,
                _ => ThrowHelper.ThrowArgumentException<double>(ExceptionMessages.InvalidDigitalOption),
            };
        }
    }

    protected override void SetBoundaryConditions(DigitalOption option, BsmModelParameters parameters)
    {
        double r = parameters.RiskFreeRate;
        double q = parameters.DividendYield;

        double maxTime = TimeVector[^1];
        double minPrice = PriceVector[0];
        double maxPrice = PriceVector[^1];

        for (int i = 0; i < TimeVector.Length - 1; i++)
        {
            double tau = maxTime - TimeVector[i];
            double dfr = Math.Exp(-r * tau);
            double dfq = Math.Exp(-q * tau);

            switch (option)
            {
                case CashOrNothingOption con:
                    switch (option.OptionType)
                    {
                        case OptionType.Call:
                            ValueMatrixSpan[i, 0] = 0.0;
                            ValueMatrixSpan[i, ^1] = con.Rebate * dfr;
                            break;
                        case OptionType.Put:
                            ValueMatrixSpan[i, 0] = con.Rebate * dfr;
                            ValueMatrixSpan[i, ^1] = 0.0;
                            break;
                        default:
                            ThrowHelper.ThrowArgumentException(ExceptionMessages.InvalidOptionType);
                            break;
                    }

                    break;

                case AssetOrNothingOption:
                    switch (option.OptionType)
                    {
                        case OptionType.Call:
                            ValueMatrixSpan[i, 0] = 0.0;
                            ValueMatrixSpan[i, ^1] = maxPrice * dfq;
                            break;
                        case OptionType.Put:
                            ValueMatrixSpan[i, 0] = minPrice * dfq;
                            ValueMatrixSpan[i, ^1] = 0.0;
                            break;
                        default:
                            ThrowHelper.ThrowArgumentException(ExceptionMessages.InvalidOptionType);
                            break;
                    }

                    break;

                default:
                    ThrowHelper.ThrowArgumentException(ExceptionMessages.InvalidDigitalOption);
                    break;
            }
        }
    }

    protected override void ApplyStepConditions(int i, DigitalOption option, BsmModelParameters parameters)
    {
    }
}
