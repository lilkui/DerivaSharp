using DerivaSharp.Instruments;
using DerivaSharp.Models;
using DerivaSharp.Time;
using TorchSharp;

namespace DerivaSharp.PricingEngines;

/// <summary>
///     Pricing engine for European options using Monte Carlo simulation.
/// </summary>
/// <param name="pathCount">The number of simulation paths.</param>
/// <param name="stepCount">The number of time steps per path.</param>
/// <param name="useCuda"><see langword="true" /> to use CUDA for GPU acceleration; otherwise, <see langword="false" />.</param>
/// <param name="seed">The optional random seed used to make generated samples deterministic.</param>
public sealed class McEuropeanEngine(int pathCount, int stepCount, bool useCuda = false, int? seed = null) : BsmPricingEngine<EuropeanOption>
{
    private readonly torch.Device _device = TorchUtils.GetDevice(useCuda);

    /// <summary>
    ///     Computes the option value using a provided random number source.
    /// </summary>
    /// <param name="option">The option to price.</param>
    /// <param name="context">The pricing context.</param>
    /// <param name="source">The random number source.</param>
    /// <returns>The option value.</returns>
    public double Value(EuropeanOption option, in PricingContext<BsmModelParameters> context, RandomNumberSource source)
    {
        BsmModelParameters parameters = context.ModelParameters;
        double tau = DayCounter.YearFraction(context.ValuationDate, option.ExpirationDate);
        double dt = tau / (stepCount - 1);

        using torch.Tensor dtVector = torch.full([stepCount - 1], dt, torch.float64, _device);
        using torch.Tensor priceMatrix = PathGenerator.Generate(context, dtVector, source);

        using DisposeScope scope = torch.NewDisposeScope();

        torch.Tensor terminalPrices = priceMatrix.select(1, stepCount - 1);
        double z = (int)option.OptionType;
        double averagePayoff = torch.mean(torch.clamp_min_(z * (terminalPrices - option.StrikePrice), 0)).item<double>();

        return averagePayoff * Math.Exp(-parameters.RiskFreeRate * tau);
    }

    /// <inheritdoc/>
    protected override double CalculateValue(EuropeanOption option, in PricingContext<BsmModelParameters> context)
    {
        if (context.ValuationDate == option.ExpirationDate)
        {
            double z = (int)option.OptionType;
            return Math.Max(z * (context.AssetPrice - option.StrikePrice), 0);
        }

        using RandomNumberSource source = new(pathCount, stepCount - 1, _device, seed);
        return Value(option, context, source);
    }
}
