using CommunityToolkit.Diagnostics;
using DerivaSharp.Instruments;
using TorchSharp;

namespace DerivaSharp.PricingEngines;

public abstract class TorchSharpPricingEngine<TOption> : PricingEngine<TOption>
    where TOption : Option
{
    protected TorchSharpPricingEngine(bool useCuda)
    {
        if (useCuda && !torch.cuda.is_available())
        {
            ThrowHelper.ThrowInvalidOperationException("CUDA is not available on this system.");
        }

        Device = useCuda ? torch.CUDA : torch.CPU;
    }

    public torch.Device Device { get; }
}
