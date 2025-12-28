using DerivaSharp.Instruments;
using DerivaSharp.Models;
using TorchSharp;

namespace DerivaSharp.PricingEngines;

public sealed class McEuropeanEngine(int pathCount, int stepCount, bool useCuda = false) : BsmPricingEngine<EuropeanOption>
{
    private readonly torch.Device _device = TorchUtils.GetDevice(useCuda);

    public double Value(EuropeanOption option, PricingContext<BsmModel> context, RandomNumberSource source)
    {
        BsmModel model = context.Model;
        double tau = GetYearsToExpiration(option, context.ValuationDate);
        double dt = tau / (stepCount - 1);

        using torch.Tensor priceMatrix = PathGenerator.Generate(context.AssetPrice, model.RiskFreeRate - model.DividendYield, model.Volatility, dt, source);

        using DisposeScope scope = torch.NewDisposeScope();

        torch.Tensor terminalPrices = priceMatrix.select(1, stepCount - 1);
        double z = (int)option.OptionType;
        double averagePayoff = torch.mean(torch.clamp_min_(z * (terminalPrices - option.StrikePrice), 0)).item<double>();

        return averagePayoff * Math.Exp(-model.RiskFreeRate * tau);
    }

    protected override double CalculateValue(EuropeanOption option, BsmModel model, double assetPrice, DateOnly valuationDate)
    {
        if (valuationDate == option.ExpirationDate)
        {
            double z = (int)option.OptionType;
            return Math.Max(z * (assetPrice - option.StrikePrice), 0);
        }

        PricingContext<BsmModel> context = new(model, assetPrice, valuationDate);
        using RandomNumberSource source = new(pathCount, stepCount - 1, _device);
        return Value(option, context, source);
    }
}
