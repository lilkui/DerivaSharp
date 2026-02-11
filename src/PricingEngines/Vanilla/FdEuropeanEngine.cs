using DerivaSharp.Instruments;
using DerivaSharp.Models;

namespace DerivaSharp.PricingEngines;

/// <summary>
///     Pricing engine for European options using finite difference methods.
/// </summary>
/// <param name="scheme">The finite difference scheme to use.</param>
/// <param name="priceStepCount">The number of price steps in the grid.</param>
/// <param name="timeStepCount">The number of time steps in the grid.</param>
public sealed class FdEuropeanEngine(FiniteDifferenceScheme scheme, int priceStepCount, int timeStepCount)
    : FdVanillaEngine<EuropeanOption>(scheme, priceStepCount, timeStepCount)
{
    protected override double GetPutLowerBoundary(EuropeanOption option, double minPrice, double dfq, double dfr) =>
        option.StrikePrice * dfr - minPrice * dfq;

    protected override void ApplyStepConditions(int i, EuropeanOption option, BsmModelParameters parameters)
    {
    }
}
