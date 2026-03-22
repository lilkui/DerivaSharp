using CommunityToolkit.Diagnostics;
using DerivaSharp.Instruments;
using DerivaSharp.Models;
using TorchSharp;
using Tensor = TorchSharp.torch.Tensor;

namespace DerivaSharp.PricingEngines;

/// <summary>
///     Monte Carlo pricing engine for Phoenix autocallable options.
/// </summary>
/// <param name="pathCount">The number of simulation paths.</param>
/// <param name="useCuda"><see langword="true" /> to use CUDA for GPU acceleration; otherwise, <see langword="false" />.</param>
/// <param name="seed">The optional random seed used to make generated samples deterministic.</param>
public sealed class McPhoenixEngine(int pathCount, bool useCuda = false, int? seed = null) : McKiAutocallableEngine<PhoenixOption>(pathCount, useCuda, seed)
{
    /// <inheritdoc/>
    protected override bool IsUpTouched(PhoenixOption option) => option.BarrierTouchStatus == BarrierTouchStatus.UpTouch;

    /// <inheritdoc/>
    protected override Tensor BuildObservationAuxTensor(PhoenixOption option, ReadOnlySpan<int> futureScheduleIndices)
    {
        double[] couponBarriersArray = new double[futureScheduleIndices.Length];
        for (int i = 0; i < futureScheduleIndices.Length; i++)
        {
            couponBarriersArray[i] = option.CouponBarrierPrices[futureScheduleIndices[i]];
        }

        return torch.tensor(couponBarriersArray, torch.float64, Device);
    }

    /// <inheritdoc/>
    protected override double CalculateAveragePayoff(
        PhoenixOption option,
        in PricingContext<BsmModelParameters> context,
        Tensor priceMatrix,
        in SimulationData simData)
    {
        using DisposeScope scope = torch.NewDisposeScope();

        BsmModelParameters parameters = context.ModelParameters;
        double r = parameters.RiskFreeRate;

        Guard.IsGreaterThan(simData.ObsIdx.numel(), 0);

        KnockOutState knockOutState = BuildKnockOutState(priceMatrix, simData);

        Tensor obsIndices = torch.arange(simData.ObsIdx.size(0), torch.int64, knockOutState.ObsPrices.device).unsqueeze(0);
        Tensor includeMask = torch.where(knockOutState.HasKnockedOut.unsqueeze(1), obsIndices <= knockOutState.FirstKoIdx.unsqueeze(1), torch.ones_like(knockOutState.KoMatrix));

        Tensor couponBarrierRow = simData.ObsAux.unsqueeze(0);
        Tensor couponHit = knockOutState.ObsPrices >= couponBarrierRow;
        Tensor couponMask = couponHit.logical_and(includeMask);

        Tensor obsTimes = simData.TimeGrid.index_select(0, simData.ObsIdx);
        Tensor discountFactors = torch.exp(-r * obsTimes);
        double couponAmount = option.InitialPrice * option.CouponRate;
        Tensor totalCoupons = couponMask.to(torch.float64).mul_(couponAmount).mul_(discountFactors).sum(1);
        Tensor discountedKoPrincipal = torch.exp(-r * knockOutState.TimeToKo) * option.PrincipalRatio;

        KnockInState knockInState = BuildKnockInState(option, priceMatrix);

        double timeToMaturity = simData.TimeGrid[-1].item<double>();
        double dfFinal = Math.Exp(-r * timeToMaturity);
        Tensor loss = torch.clamp_(knockInState.FinalSpot - option.UpperStrikePrice, option.LowerStrikePrice - option.UpperStrikePrice, 0).div_(option.InitialPrice);
        Tensor discountedPrincipal = torch.full_like(totalCoupons, option.PrincipalRatio * dfFinal);
        Tensor discountedLoss = loss * dfFinal;
        Tensor maturityTailPayoff = discountedPrincipal + torch.where(knockInState.HasKnockedIn, discountedLoss, torch.zeros_like(discountedLoss));
        Tensor knockOutPayoff = totalCoupons + discountedKoPrincipal;
        Tensor maturityPayoff = totalCoupons + maturityTailPayoff;
        Tensor pathPayoffs = torch.where(knockOutState.HasKnockedOut, knockOutPayoff, maturityPayoff);

        return pathPayoffs.mean().item<double>();
    }

    /// <inheritdoc/>
    protected override double CalculateTerminalPayoff(PhoenixOption option, in PricingContext<BsmModelParameters> context)
    {
        Guard.IsEqualTo(context.ValuationDate, option.ExpirationDate);

        double loss = Math.Clamp(context.AssetPrice - option.UpperStrikePrice, option.LowerStrikePrice - option.UpperStrikePrice, 0) / option.InitialPrice;
        double coupon = context.AssetPrice >= option.CouponBarrierPrices[^1]
            ? option.InitialPrice * option.CouponRate
            : 0.0;

        if (context.AssetPrice >= option.KnockOutPrices[^1])
        {
            return option.PrincipalRatio + coupon;
        }

        if (context.AssetPrice < option.KnockInPrice || option.BarrierTouchStatus == BarrierTouchStatus.DownTouch)
        {
            return option.PrincipalRatio + coupon + loss;
        }

        return option.PrincipalRatio + coupon;
    }
}
