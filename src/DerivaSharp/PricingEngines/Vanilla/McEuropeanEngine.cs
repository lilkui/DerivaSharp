using DerivaSharp.Instruments;
using TorchSharp;

namespace DerivaSharp.PricingEngines;

public sealed class McEuropeanEngine(int pathCount, int stepCount, bool useCuda = false) : TorchSharpPricingEngine<EuropeanOption>(useCuda)
{
    public double Value(EuropeanOption option, PricingContext context, RandomNumberSource source)
    {
        double tau = GetYearsToExpiration(option, context);
        double dt = tau / (stepCount - 1);

        using torch.Tensor priceMatrix = PathGenerator.Generate(context.AssetPrice, context.RiskFreeRate - context.DividendYield, context.Volatility, dt, source);

        using DisposeScope scope = torch.NewDisposeScope();

        torch.Tensor terminalPrices = priceMatrix.select(1, stepCount - 1);
        double z = (int)option.OptionType;
        double averagePayoff = torch.mean(torch.clamp_min_(z * (terminalPrices - option.StrikePrice), 0)).item<double>();

        return averagePayoff * Math.Exp(-context.RiskFreeRate * tau);
    }

    protected override double CalculateValue(EuropeanOption option, PricingContext context)
    {
        if (context.ValuationDate == option.ExpirationDate)
        {
            double z = (int)option.OptionType;
            return Math.Max(z * (context.AssetPrice - option.StrikePrice), 0);
        }

        using RandomNumberSource source = new(pathCount, stepCount - 1, Device);
        return Value(option, context, source);
    }
}
