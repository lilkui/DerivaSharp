using DerivaSharp.Instruments;
using DerivaSharp.Models;

namespace DerivaSharp.PricingEngines;

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
