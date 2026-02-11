using DerivaSharp.Instruments;
using DerivaSharp.Models;

namespace DerivaSharp.PricingEngines;

/// <summary>
///     Pricing engine for American options using finite difference methods with early exercise.
/// </summary>
/// <param name="scheme">The finite difference scheme to use.</param>
/// <param name="priceStepCount">The number of price steps in the grid.</param>
/// <param name="timeStepCount">The number of time steps in the grid.</param>
public sealed class FdAmericanEngine(FiniteDifferenceScheme scheme, int priceStepCount, int timeStepCount)
    : FdVanillaEngine<AmericanOption>(scheme, priceStepCount, timeStepCount)
{
    protected override double GetPutLowerBoundary(AmericanOption option, double minPrice, double dfq, double dfr) =>
        option.StrikePrice;

    protected override void ApplyStepConditions(int i, AmericanOption option, BsmModelParameters parameters)
    {
        double x = option.StrikePrice;
        int z = (int)option.OptionType;

        Span<double> values = ValueMatrixSpan.GetRowSpan(i);
        for (int j = 0; j < PriceVector.Length; j++)
        {
            double payoff = Math.Max(z * (PriceVector[j] - x), 0);
            values[j] = Math.Max(values[j], payoff);
        }
    }
}
