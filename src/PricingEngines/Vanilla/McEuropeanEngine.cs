using DerivaSharp.Instruments;
using DerivaSharp.Models;
using TorchSharp;

namespace DerivaSharp.PricingEngines;

public sealed class McEuropeanEngine(int pathCount, int stepCount, bool useCuda = false) : BsmPricingEngine<EuropeanOption>
{
    private readonly torch.Device _device = TorchUtils.GetDevice(useCuda);

    public double Value(EuropeanOption option, PricingContext<BsmModelParameters> context, RandomNumberSource source)
    {
        BsmModelParameters parameters = context.ModelParameters;
        double tau = GetYearsToExpiration(option, context.ValuationDate);
        double dt = tau / (stepCount - 1);

        using torch.Tensor dtVector = torch.full([stepCount - 1], dt, torch.float64, _device);
        using torch.Tensor priceMatrix = PathGenerator.Generate(context, dtVector, source);

        using DisposeScope scope = torch.NewDisposeScope();

        torch.Tensor terminalPrices = priceMatrix.select(1, stepCount - 1);
        double z = (int)option.OptionType;
        double averagePayoff = torch.mean(torch.clamp_min_(z * (terminalPrices - option.StrikePrice), 0)).item<double>();

        return averagePayoff * Math.Exp(-parameters.RiskFreeRate * tau);
    }

    protected override double CalculateValue(EuropeanOption option, BsmModelParameters parameters, double assetPrice, DateOnly valuationDate)
    {
        if (valuationDate == option.ExpirationDate)
        {
            double z = (int)option.OptionType;
            return Math.Max(z * (assetPrice - option.StrikePrice), 0);
        }

        PricingContext<BsmModelParameters> context = new(parameters, assetPrice, valuationDate);
        using RandomNumberSource source = new(pathCount, stepCount - 1, _device);
        return Value(option, context, source);
    }
}
