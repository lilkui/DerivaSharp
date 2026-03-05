using CommunityToolkit.Diagnostics;
using DerivaSharp.Instruments;
using DerivaSharp.Models;
using TorchSharp;
using Tensor = TorchSharp.torch.Tensor;

namespace DerivaSharp.PricingEngines;

/// <summary>
///     Pricing engine for snowball options using Monte Carlo simulation.
/// </summary>
/// <param name="pathCount">The number of simulation paths.</param>
/// <param name="useCuda">Whether to use CUDA for GPU acceleration.</param>
/// <param name="seed">The optional random seed used to make generated samples deterministic.</param>
public sealed class McSnowballEngine(int pathCount, bool useCuda = false, int? seed = null) : McKiAutocallableEngine<SnowballOption>(pathCount, useCuda, seed)
{
    protected override bool IsUpTouched(SnowballOption option)
    {
        return option.BarrierTouchStatus == BarrierTouchStatus.UpTouch;
    }

    protected override Tensor BuildObservationAuxTensor(SnowballOption option, ReadOnlySpan<int> futureScheduleIndices)
    {
        double[] koCouponRatesArray = new double[futureScheduleIndices.Length];
        for (int i = 0; i < futureScheduleIndices.Length; i++)
        {
            koCouponRatesArray[i] = option.KnockOutCouponRates[futureScheduleIndices[i]];
        }

        return torch.tensor(koCouponRatesArray, torch.float64, Device);
    }

    protected override double CalculateAveragePayoff(
        SnowballOption option,
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
        Tensor discountedKoPayoff = pathKoCouponRate * couponAccrualTime * torch.exp(-r * knockOutState.TimeToKo);

        KnockInState knockInState = BuildKnockInState(option, priceMatrix);

        double timeToMaturity = simData.TimeGrid[-1].item<double>();
        double dfFinal = Math.Exp(-r * timeToMaturity);
        double maturityCouponPayoff = option.MaturityCouponRate * (option.ExpirationDate.DayNumber - option.EffectiveDate.DayNumber) / 365.0;

        Tensor loss = torch.clamp_(knockInState.FinalSpot - option.UpperStrikePrice, option.LowerStrikePrice - option.UpperStrikePrice, 0).div_(option.InitialPrice);
        Tensor discountedMaturityPayoff = torch.where(knockInState.HasKnockedIn.logical_not(), maturityCouponPayoff, loss) * dfFinal;

        Tensor pathPayoffs = torch.where(knockOutState.HasKnockedOut, discountedKoPayoff, discountedMaturityPayoff);

        return pathPayoffs.mean().item<double>();
    }

    protected override double CalculateTerminalPayoff(SnowballOption option, in PricingContext<BsmModelParameters> context)
    {
        Guard.IsEqualTo(context.ValuationDate, option.ExpirationDate);

        double t = (option.ExpirationDate.DayNumber - option.EffectiveDate.DayNumber) / 365.0;
        double loss = Math.Clamp(context.AssetPrice - option.UpperStrikePrice, option.LowerStrikePrice - option.UpperStrikePrice, 0) / option.InitialPrice;

        if (context.AssetPrice >= option.KnockOutPrices[^1])
        {
            return option.KnockOutCouponRates[^1] * t;
        }

        if (context.AssetPrice < option.KnockInPrice || option.BarrierTouchStatus == BarrierTouchStatus.DownTouch)
        {
            return loss;
        }

        return option.MaturityCouponRate * t;
    }
}
