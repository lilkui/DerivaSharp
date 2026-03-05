using DerivaSharp.Instruments;
using TorchSharp;
using Tensor = TorchSharp.torch.Tensor;

namespace DerivaSharp.PricingEngines;

/// <summary>
///     Base class for Monte Carlo pricing engines of knock-in autocallable notes.
/// </summary>
/// <typeparam name="TOption">The type of knock-in autocallable option.</typeparam>
/// <param name="pathCount">The number of simulation paths.</param>
/// <param name="useCuda">Whether to use CUDA for GPU acceleration.</param>
/// <param name="seed">The optional random seed used to make generated samples deterministic.</param>
public abstract class McKiAutocallableEngine<TOption>(int pathCount, bool useCuda = false, int? seed = null)
    : McAutocallableEngine<TOption>(pathCount, useCuda, seed)
    where TOption : KiAutocallableNote
{
    /// <summary>
    ///     Builds knock-in state tensors shared by knock-in autocallable payoff implementations.
    /// </summary>
    /// <param name="option">The option being priced.</param>
    /// <param name="priceMatrix">Simulated paths with shape [pathCount, stepCount].</param>
    /// <returns>
    ///     A <see cref="KnockInState" /> containing terminal spot and final knock-in mask, including historical
    ///     barrier-touch override behavior.
    /// </returns>
    protected static KnockInState BuildKnockInState(TOption option, Tensor priceMatrix)
    {
        Tensor finalSpot = priceMatrix.select(1, -1);
        Tensor hasKnockedInOnFuturePath = option.KnockInObservationFrequency == ObservationFrequency.AtExpiry
            ? finalSpot < option.KnockInPrice
            : (priceMatrix < option.KnockInPrice).any(1);
        Tensor hasKnockedIn = option.BarrierTouchStatus == BarrierTouchStatus.DownTouch
            ? torch.ones_like(hasKnockedInOnFuturePath)
            : hasKnockedInOnFuturePath;

        return new KnockInState(finalSpot, hasKnockedIn);
    }

    /// <summary>
    ///     Knock-in state tensors derived from simulated paths.
    /// </summary>
    protected readonly record struct KnockInState(Tensor FinalSpot, Tensor HasKnockedIn);
}
