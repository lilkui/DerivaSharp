using CommunityToolkit.Diagnostics;
using TorchSharp;

namespace DerivaSharp.PricingEngines;

/// <summary>
///     Provides utility methods for TorchSharp device management.
/// </summary>
internal static class TorchUtils
{
    /// <summary>
    ///     Gets the appropriate torch device based on CUDA availability.
    /// </summary>
    /// <param name="useCuda"><see langword="true" /> to use CUDA when available; otherwise, <see langword="false" />.</param>
    /// <returns>A torch device (CUDA or CPU).</returns>
    /// <exception cref="InvalidOperationException">CUDA is requested but not available.</exception>
    public static torch.Device GetDevice(bool useCuda)
    {
        if (useCuda && !torch.cuda.is_available())
        {
            ThrowHelper.ThrowInvalidOperationException(ExceptionMessages.CudaUnavailable);
        }

        return useCuda ? torch.CUDA : torch.CPU;
    }
}
