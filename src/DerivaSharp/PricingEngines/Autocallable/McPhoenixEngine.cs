using CommunityToolkit.Diagnostics;
using DerivaSharp.Instruments;
using DerivaSharp.Models;
using DerivaSharp.Time;
using TorchSharp;
using Tensor = TorchSharp.torch.Tensor;

namespace DerivaSharp.PricingEngines;

public sealed class McPhoenixEngine(int pathCount, bool useCuda = false) : BsmPricingEngine<PhoenixOption>
{
    private readonly torch.Device _device = TorchUtils.GetDevice(useCuda);

    public double[] Values(PhoenixOption option, PricingContext<BsmModel> context, double[] assetPrices)
    {
        if (option.BarrierTouchStatus == BarrierTouchStatus.UpTouch)
        {
            return new double[assetPrices.Length];
        }

        int count = assetPrices.Length;
        Guard.IsGreaterThanOrEqualTo(count, 3);

        using DisposeScope scope = torch.NewDisposeScope();

        SimulationParameters parameters = PrepareSimulationParameters(option, context);

        if (parameters.StepCount <= 0)
        {
            return assetPrices.Select(s => CalculateTerminalPayoff(option, context with { AssetPrice = s })).ToArray();
        }

        using RandomNumberSource source = new(pathCount, parameters.StepCount, _device);

        double[] values = new double[count];
        for (int i = 0; i < count; i++)
        {
            PricingContext<BsmModel> assetContext = context with { AssetPrice = assetPrices[i] };
            using Tensor priceMatrix = CreatePriceMatrix(assetContext, parameters.DtVector, source);
            values[i] = CalculateAveragePayoff(option, assetContext, priceMatrix, parameters);
        }

        return values;
    }

    public double[] Deltas(PhoenixOption option, PricingContext<BsmModel> context, double[] assetPrices)
    {
        double[] values = Values(option, context, assetPrices);

        using DisposeScope scope = torch.NewDisposeScope();

        Tensor valueTensor = torch.tensor(values, torch.float64, _device);
        Tensor deltaTensor = torch.zeros_like(valueTensor);

        double ds = assetPrices[1] - assetPrices[0];
        deltaTensor[1..^1] = (valueTensor[2..] - valueTensor[..^2]) / (2 * ds);
        deltaTensor[0] = (valueTensor[1] - valueTensor[0]) / ds;
        deltaTensor[^1] = (valueTensor[^1] - valueTensor[^2]) / ds;

        return deltaTensor.cpu().data<double>().ToArray();
    }

    public double[] Gammas(PhoenixOption option, PricingContext<BsmModel> context, double[] assetPrices)
    {
        double[] values = Values(option, context, assetPrices);

        using DisposeScope scope = torch.NewDisposeScope();

        Tensor valueTensor = torch.tensor(values, torch.float64, _device);
        Tensor gammaTensor = torch.zeros_like(valueTensor);

        double ds = assetPrices[1] - assetPrices[0];
        gammaTensor[1..^1] = (valueTensor[2..] - 2 * valueTensor[1..^1] + valueTensor[..^2]) / (ds * ds);
        gammaTensor[0] = (valueTensor[2] - 2 * valueTensor[1] + valueTensor[0]) / (ds * ds);
        gammaTensor[^1] = (valueTensor[^1] - 2 * valueTensor[^2] + valueTensor[^3]) / (ds * ds);

        return gammaTensor.cpu().data<double>().ToArray();
    }

    protected override double CalculateValue(PhoenixOption option, BsmModel model, double assetPrice, DateOnly valuationDate)
    {
        if (option.BarrierTouchStatus == BarrierTouchStatus.UpTouch)
        {
            return 0.0;
        }

        PricingContext<BsmModel> context = new(model, assetPrice, valuationDate);
        using DisposeScope scope = torch.NewDisposeScope();

        SimulationParameters parameters = PrepareSimulationParameters(option, context);

        if (parameters.StepCount <= 0)
        {
            return CalculateTerminalPayoff(option, context);
        }

        using RandomNumberSource source = new(pathCount, parameters.StepCount, _device);
        Tensor priceMatrix = CreatePriceMatrix(context, parameters.DtVector, source);

        return CalculateAveragePayoff(option, context, priceMatrix, parameters);
    }

    private static Tensor CreatePriceMatrix(PricingContext<BsmModel> context, Tensor dtVector, RandomNumberSource source)
    {
        BsmModel model = context.Model;
        return PathGenerator.Generate(context.AssetPrice, model.RiskFreeRate - model.DividendYield, model.Volatility, dtVector, source);
    }

    private static double CalculateAveragePayoff(
        PhoenixOption option,
        PricingContext<BsmModel> context,
        Tensor priceMatrix,
        in SimulationParameters parameters)
    {
        using DisposeScope scope = torch.NewDisposeScope();

        BsmModel model = context.Model;
        double r = model.RiskFreeRate;

        Guard.IsGreaterThan(parameters.ObsIdx.numel(), 0);

        Tensor obsPrices = priceMatrix.index_select(1, parameters.ObsIdx);
        Tensor koPriceRow = parameters.KoPrices.unsqueeze(0);
        Tensor koMatrix = obsPrices >= koPriceRow;
        Tensor hasKnockedOut = koMatrix.any(1);
        Tensor firstKoIdx = koMatrix.@long().argmax(1);

        Tensor obsIndices = torch.arange(parameters.ObsIdx.size(0), torch.int64, obsPrices.device).unsqueeze(0);
        Tensor includeMask = torch.where(hasKnockedOut.unsqueeze(1), obsIndices <= firstKoIdx.unsqueeze(1), torch.ones_like(koMatrix));

        Tensor couponBarrierRow = parameters.CouponBarriers.unsqueeze(0);
        Tensor couponHit = obsPrices >= couponBarrierRow;
        Tensor couponMask = couponHit.logical_and(includeMask);

        Tensor obsTimes = parameters.TimeGrid.index_select(0, parameters.ObsIdx);
        Tensor discountFactors = torch.exp(-r * obsTimes);
        double couponAmount = option.InitialPrice * option.CouponRate;
        Tensor totalCoupons = couponMask.to(torch.float64).mul_(couponAmount).mul_(discountFactors).sum(1);

        Tensor finalSpot = priceMatrix.select(1, -1);
        Tensor hasKnockedInOnFuturePath = option.KnockInObservationFrequency == ObservationFrequency.AtExpiry
            ? finalSpot < option.KnockInPrice
            : (priceMatrix < option.KnockInPrice).any(1);
        Tensor hasKnockedIn = option.BarrierTouchStatus == BarrierTouchStatus.DownTouch
            ? torch.ones_like(hasKnockedInOnFuturePath)
            : hasKnockedInOnFuturePath;

        double timeToMaturity = parameters.TimeGrid[-1].item<double>();
        double dfFinal = Math.Exp(-r * timeToMaturity);
        Tensor loss = torch.clamp_(finalSpot - option.UpperStrikePrice, option.LowerStrikePrice - option.UpperStrikePrice, 0).div_(option.InitialPrice);
        Tensor discountedMaturityPayoff = loss * dfFinal;

        Tensor pathPayoffs = torch.where(hasKnockedOut, totalCoupons, torch.where(hasKnockedIn.logical_not(), totalCoupons, totalCoupons + discountedMaturityPayoff));

        return pathPayoffs.mean().item<double>();
    }

    private static double CalculateTerminalPayoff(PhoenixOption option, PricingContext<BsmModel> context)
    {
        Guard.IsEqualTo(context.ValuationDate, option.ExpirationDate);

        double loss = Math.Clamp(context.AssetPrice - option.UpperStrikePrice, option.LowerStrikePrice - option.UpperStrikePrice, 0) / option.InitialPrice;
        double coupon = context.AssetPrice >= option.CouponBarrierPrices[^1]
            ? option.InitialPrice * option.CouponRate
            : 0.0;

        if (context.AssetPrice >= option.KnockOutPrices[^1])
        {
            return coupon;
        }

        if (context.AssetPrice < option.KnockInPrice || option.BarrierTouchStatus == BarrierTouchStatus.DownTouch)
        {
            return coupon + loss;
        }

        return coupon;
    }

    private SimulationParameters PrepareSimulationParameters(PhoenixOption option, PricingContext<BsmModel> context)
    {
        DateOnly valuationDate = context.ValuationDate;
        Guard.IsBetweenOrEqualTo(valuationDate, option.EffectiveDate, option.ExpirationDate);

        DateOnly[] futureTradingDays = DateUtils.GetTradingDays(valuationDate, option.ExpirationDate).ToArray();
        if (futureTradingDays.Length <= 1)
        {
            return new SimulationParameters(
                torch.empty(0, torch.float64, _device),
                torch.empty(0, torch.float64, _device),
                torch.empty(0, torch.int64, _device),
                torch.empty(0, torch.float64, _device),
                torch.empty(0, torch.float64, _device),
                0);
        }

        int stepCount = futureTradingDays.Length - 1;

        double[] yearFractions = new double[futureTradingDays.Length];
        int t0 = valuationDate.DayNumber;
        for (int i = 0; i < futureTradingDays.Length; i++)
        {
            yearFractions[i] = (futureTradingDays[i].DayNumber - t0) / 365.0;
        }

        double[] dtArray = new double[stepCount];
        for (int i = 0; i < stepCount; i++)
        {
            dtArray[i] = yearFractions[i + 1] - yearFractions[i];
        }

        Tensor timeGrid = torch.tensor(yearFractions, torch.float64, _device);
        Tensor dtVector = torch.tensor(dtArray, torch.float64, _device);

        DateOnly[] futureObsDates = option.KnockOutObservationDates.Where(d => d >= valuationDate).ToArray();

        Dictionary<DateOnly, (double KoPrice, double CouponBarrier)> obsMap = new(option.KnockOutObservationDates.Length);
        for (int i = 0; i < option.KnockOutObservationDates.Length; i++)
        {
            obsMap[option.KnockOutObservationDates[i]] = (option.KnockOutPrices[i], option.CouponBarrierPrices[i]);
        }

        int n = futureObsDates.Length;
        int[] obsIdxArray = new int[n];
        double[] koPricesArray = new double[n];
        double[] couponBarriersArray = new double[n];

        for (int i = 0; i < futureObsDates.Length; i++)
        {
            int index = futureTradingDays.AsSpan().BinarySearch(futureObsDates[i]);
            if (index < 0)
            {
                ThrowHelper.ThrowInvalidOperationException($"Observation date {futureObsDates[i]} is not a trading day.");
            }

            obsIdxArray[i] = index;
            (double koPrice, double couponBarrier) = obsMap[futureObsDates[i]];
            koPricesArray[i] = koPrice;
            couponBarriersArray[i] = couponBarrier;
        }

        Tensor obsIdx = torch.tensor(obsIdxArray, torch.int64, _device);
        Tensor koPrices = torch.tensor(koPricesArray, torch.float64, _device);
        Tensor couponBarriers = torch.tensor(couponBarriersArray, torch.float64, _device);

        return new SimulationParameters(timeGrid, dtVector, obsIdx, koPrices, couponBarriers, stepCount);
    }

    private readonly record struct SimulationParameters(Tensor TimeGrid, Tensor DtVector, Tensor ObsIdx, Tensor KoPrices, Tensor CouponBarriers, int StepCount);
}
