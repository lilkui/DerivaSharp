using CommunityToolkit.Diagnostics;
using TorchSharp;

namespace DerivaSharp.PricingEngines;

internal static class TorchUtils
{
    public static torch.Device GetDevice(bool useCuda)
    {
        if (useCuda && !torch.cuda.is_available())
        {
            ThrowHelper.ThrowInvalidOperationException(ExceptionMessages.CudaUnavailable);
        }

        return useCuda ? torch.CUDA : torch.CPU;
    }
}
