using CommunityToolkit.Diagnostics;
using DerivaSharp.Instruments;
using DerivaSharp.Models;
using TorchSharp;
using Tensor = TorchSharp.torch.Tensor;

namespace DerivaSharp.PricingEngines;

/// <summary>
///     Monte Carlo pricing engine for binary snowball options.
/// </summary>
/// <param name="pathCount">The number of simulation paths.</param>
/// <param name="useCuda">Whether to use CUDA for GPU acceleration.</param>
/// <param name="seed">The optional random seed used to make generated samples deterministic.</param>
public sealed class McBinarySnowballEngine(int pathCount, bool useCuda = false, int? seed = null) : McAutocallableEngine<BinarySnowballOption>(pathCount, useCuda, seed)
{
    protected override bool IsUpTouched(BinarySnowballOption option)
    {
        return option.BarrierTouchStatus == BarrierTouchStatus.UpTouch;
    }

    protected override Tensor BuildObservationAuxTensor(BinarySnowballOption option, ReadOnlySpan<int> futureScheduleIndices)
    {
        double[] koCouponRatesArray = new double[futureScheduleIndices.Length];
        for (int i = 0; i < futureScheduleIndices.Length; i++)
        {
            koCouponRatesArray[i] = option.KnockOutCouponRates[futureScheduleIndices[i]];
        }

        return torch.tensor(koCouponRatesArray, torch.float64, Device);
    }

    protected override double CalculateAveragePayoff(
        BinarySnowballOption option,
        in PricingContext<BsmModelParameters> context,
        Tensor priceMatrix,
        in SimulationData simData)
    {
        using DisposeScope scope = torch.NewDisposeScope();

        BsmModelParameters parameters = context.ModelParameters;
        double r = parameters.RiskFreeRate;

        KnockOutState knockOutState = BuildKnockOutState(priceMatrix, simData);
        double timeFromEffectiveToValuation = (context.ValuationDate.DayNumber - option.EffectiveDate.DayNumber) / 365.0;
        Tensor couponAccrualTime = knockOutState.TimeToKo + timeFromEffectiveToValuation;

        Tensor pathKoCouponRate = simData.ObsAux.index_select(0, knockOutState.FirstKoIdx);
        Tensor discountedKoPayoff = (pathKoCouponRate * couponAccrualTime + option.PrincipalRatio) * torch.exp(-r * knockOutState.TimeToKo);

        double timeToMaturity = simData.TimeGrid[-1].item<double>();
        double dfFinal = Math.Exp(-r * timeToMaturity);
        double maturityCouponPayoff = option.PrincipalRatio + option.MaturityCouponRate * (option.ExpirationDate.DayNumber - option.EffectiveDate.DayNumber) / 365.0;
        Tensor discountedMaturityPayoff = torch.full_like(discountedKoPayoff, maturityCouponPayoff * dfFinal);

        Tensor pathPayoffs = torch.where(knockOutState.HasKnockedOut, discountedKoPayoff, discountedMaturityPayoff);

        return pathPayoffs.mean().item<double>();
    }

    protected override double CalculateTerminalPayoff(BinarySnowballOption option, in PricingContext<BsmModelParameters> context)
    {
        Guard.IsEqualTo(context.ValuationDate, option.ExpirationDate);

        double t = (option.ExpirationDate.DayNumber - option.EffectiveDate.DayNumber) / 365.0;

        if (context.AssetPrice >= option.KnockOutPrices[^1])
        {
            return option.PrincipalRatio + option.KnockOutCouponRates[^1] * t;
        }

        return option.PrincipalRatio + option.MaturityCouponRate * t;
    }
}
