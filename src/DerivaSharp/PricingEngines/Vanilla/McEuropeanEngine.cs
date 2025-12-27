using DerivaSharp.Instruments;
using DerivaSharp.Models;
using TorchSharp;

namespace DerivaSharp.PricingEngines;

public sealed class McEuropeanEngine(int pathCount, int stepCount, bool useCuda = false) : BsmPricingEngine<EuropeanOption>
{
    private readonly torch.Device _device = TorchHelper.GetDevice(useCuda);

    public double Value(EuropeanOption option, PricingContext context, BsmModel model, MarketData market, RandomNumberSource source)
    {
        double tau = GetYearsToExpiration(option, context);
        double dt = tau / (stepCount - 1);

        using torch.Tensor priceMatrix = PathGenerator.Generate(market.AssetPrice, model.RiskFreeRate - model.DividendYield, model.Volatility, dt, source);

        using DisposeScope scope = torch.NewDisposeScope();

        torch.Tensor terminalPrices = priceMatrix.select(1, stepCount - 1);
        double z = (int)option.OptionType;
        double averagePayoff = torch.mean(torch.clamp_min_(z * (terminalPrices - option.StrikePrice), 0)).item<double>();

        return averagePayoff * Math.Exp(-model.RiskFreeRate * tau);
    }

    protected override double CalculateValue(EuropeanOption option, BsmModel model, MarketData market, PricingContext context)
    {
        if (context.ValuationDate == option.ExpirationDate)
        {
            double z = (int)option.OptionType;
            return Math.Max(z * (market.AssetPrice - option.StrikePrice), 0);
        }

        using RandomNumberSource source = new(pathCount, stepCount - 1, _device);
        return Value(option, context, model, market, source);
    }
}
