using CommunityToolkit.Diagnostics;
using DerivaSharp.Instruments;
using DerivaSharp.Models;
using TorchSharp;
using Tensor = TorchSharp.torch.Tensor;

namespace DerivaSharp.PricingEngines;

/// <summary>
///     Monte Carlo pricing engine for ternary snowball options.
/// </summary>
public sealed class McTernarySnowballEngine(int pathCount, bool useCuda = false) : McKiAutocallableEngine<TernarySnowballOption>(pathCount, useCuda)
{
    protected override bool IsUpTouched(TernarySnowballOption option)
    {
        return option.BarrierTouchStatus == BarrierTouchStatus.UpTouch;
    }

    protected override Tensor BuildObservationAuxTensor(TernarySnowballOption option, ReadOnlySpan<int> futureScheduleIndices)
    {
        double[] koCouponRatesArray = new double[futureScheduleIndices.Length];
        for (int i = 0; i < futureScheduleIndices.Length; i++)
        {
            koCouponRatesArray[i] = option.KnockOutCouponRates[futureScheduleIndices[i]];
        }

        return torch.tensor(koCouponRatesArray, torch.float64, Device);
    }

    protected override double CalculateAveragePayoff(
        TernarySnowballOption option,
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
        double tenor = (option.ExpirationDate.DayNumber - option.EffectiveDate.DayNumber) / 365.0;
        double maturityCouponPayoff = option.MaturityCouponRate * tenor;
        double minimalCouponPayoff = option.MinimalCouponRate * tenor;

        Tensor discountedMaturityPayoff = torch.where(knockInState.HasKnockedIn, minimalCouponPayoff, maturityCouponPayoff) * dfFinal;

        Tensor pathPayoffs = torch.where(knockOutState.HasKnockedOut, discountedKoPayoff, discountedMaturityPayoff);

        return pathPayoffs.mean().item<double>();
    }

    protected override double CalculateTerminalPayoff(TernarySnowballOption option, in PricingContext<BsmModelParameters> context)
    {
        Guard.IsEqualTo(context.ValuationDate, option.ExpirationDate);

        double t = (option.ExpirationDate.DayNumber - option.EffectiveDate.DayNumber) / 365.0;

        if (context.AssetPrice >= option.KnockOutPrices[^1])
        {
            return option.KnockOutCouponRates[^1] * t;
        }

        if (context.AssetPrice < option.KnockInPrice || option.BarrierTouchStatus == BarrierTouchStatus.DownTouch)
        {
            return option.MinimalCouponRate * t;
        }

        return option.MaturityCouponRate * t;
    }
}
