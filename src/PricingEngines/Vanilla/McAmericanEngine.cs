using CommunityToolkit.Diagnostics;
using DerivaSharp.Instruments;
using DerivaSharp.Models;
using TorchSharp;

namespace DerivaSharp.PricingEngines;

/// <summary>
///     Pricing engine for American options using Monte Carlo simulation with Longstaff-Schwartz regression.
/// </summary>
/// <param name="pathCount">The number of simulation paths.</param>
/// <param name="stepCount">The number of time steps per path.</param>
/// <param name="useCuda">Whether to use CUDA for GPU acceleration.</param>
public sealed class McAmericanEngine(int pathCount, int stepCount, bool useCuda = false) : BsmPricingEngine<AmericanOption>
{
    private readonly torch.Device _device = TorchUtils.GetDevice(useCuda);

    /// <summary>
    ///     Computes the option value using a provided random number source.
    /// </summary>
    /// <param name="option">The option to price.</param>
    /// <param name="context">The pricing context.</param>
    /// <param name="source">The random number source.</param>
    /// <returns>The option value.</returns>
    public double Value(AmericanOption option, PricingContext<BsmModelParameters> context, RandomNumberSource source)
    {
        Guard.IsGreaterThan(stepCount, 2);

        BsmModelParameters parameters = context.ModelParameters;
        double x = option.StrikePrice;
        int z = (int)option.OptionType;
        double tau = GetYearsToExpiration(option, context.ValuationDate);
        double dt = tau / (stepCount - 1);
        double df = Math.Exp(-parameters.RiskFreeRate * dt);

        using torch.Tensor dtVector = torch.full([stepCount - 1], dt, torch.float64, _device);
        using torch.Tensor priceMatrix = PathGenerator.Generate(context, dtVector, source);

        using DisposeScope scope = torch.NewDisposeScope();

        torch.Tensor cashFlows = (z * (priceMatrix.select(1, stepCount - 1) - x)).clamp_min_(0);

        for (int j = stepCount - 2; j >= 1; j--)
        {
            cashFlows.mul_(df);

            torch.Tensor priceAtStep = priceMatrix.select(1, j);
            torch.Tensor intrinsicValues = (z * (priceAtStep - x)).clamp_min_(0);
            torch.Tensor itmMask = intrinsicValues > 0;

            long itmCount = itmMask.sum().item<long>();
            if (itmCount <= 2)
            {
                continue;
            }

            torch.Tensor itmPathsX = priceAtStep.masked_select(itmMask);
            torch.Tensor itmPathsY = cashFlows.masked_select(itmMask);

            torch.Tensor vandermondeMatrix = torch.vander(itmPathsX, 3, true);
            torch.Tensor p = torch.linalg.lstsq(vandermondeMatrix, itmPathsY).Solution;
            torch.Tensor continuationValues = torch.matmul(vandermondeMatrix, p);

            torch.Tensor exerciseMask = intrinsicValues.masked_select(itmMask) > continuationValues;
            cashFlows[itmMask] = torch.where(exerciseMask, intrinsicValues.masked_select(itmMask), itmPathsY);
        }

        double average = torch.mean(cashFlows).item<double>();
        return average * df;
    }

    protected override double CalculateValue(AmericanOption option, BsmModelParameters parameters, double assetPrice, DateOnly valuationDate)
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
