using DerivaSharp.Instruments;
using DerivaSharp.Models;
using TorchSharp;

namespace DerivaSharp.PricingEngines;

/// <summary>
///     Pricing engine for European options using Monte Carlo simulation.
/// </summary>
/// <param name="pathCount">The number of simulation paths.</param>
/// <param name="stepCount">The number of time steps per path.</param>
/// <param name="useCuda">Whether to use CUDA for GPU acceleration.</param>
public sealed class McEuropeanEngine(int pathCount, int stepCount, bool useCuda = false) : BsmPricingEngine<EuropeanOption>
{
    private readonly torch.Device _device = TorchUtils.GetDevice(useCuda);

    /// <summary>
    ///     Computes the option value using a provided random number source.
    /// </summary>
    /// <param name="option">The option to price.</param>
    /// <param name="context">The pricing context.</param>
    /// <param name="source">The random number source.</param>
    /// <returns>The option value.</returns>
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
