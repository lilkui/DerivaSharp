using DerivaSharp.Instruments;
using DerivaSharp.Models;

namespace DerivaSharp.PricingEngines;

public sealed class FdEuropeanEngine(FiniteDifferenceScheme scheme, int priceStepCount, int timeStepCount)
    : FdVanillaEngine<EuropeanOption>(scheme, priceStepCount, timeStepCount)
{
    protected override double GetPutLowerBoundary(EuropeanOption option, double minPrice, double dfq, double dfr) =>
        option.StrikePrice * dfr - minPrice * dfq;

    protected override void ApplyStepConditions(int i, EuropeanOption option, BsmModelParameters parameters)
    {
    }
}
