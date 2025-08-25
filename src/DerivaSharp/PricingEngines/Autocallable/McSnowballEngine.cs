using CommunityToolkit.Diagnostics;
using DerivaSharp.Instruments;
using DerivaSharp.Time;
using TorchSharp;

namespace DerivaSharp.PricingEngines;

public sealed class McSnowballEngine(int pathCount, bool useCuda = false) : TorchSharpPricingEngine<SnowballOption>(useCuda)
{
    public double[] Values(SnowballOption option, PricingContext context, double[] assetPrices)
    {
        if (option.BarrierTouchStatus == BarrierTouchStatus.UpTouch)
        {
            return new double[assetPrices.Length];
        }

        int count = assetPrices.Length;
        Guard.IsGreaterThanOrEqualTo(count, 3);

        using DisposeScope scope = torch.NewDisposeScope();

        (torch.Tensor timeGrid, torch.Tensor dtVector, torch.Tensor obsIdx, int stepCount) = PrepareSimulationParameters(option, context);

        if (stepCount <= 0)
        {
            return assetPrices.Select(s => CalculateTerminalPayoff(option, context with { AssetPrice = s })).ToArray();
        }

        using RandomNumberSource source = new(pathCount, stepCount, Device);

        double[] values = new double[count];
        for (int i = 0; i < count; i++)
        {
            using torch.Tensor priceMatrix = CreatePriceMatrix(context with { AssetPrice = assetPrices[i] }, dtVector, source);
            values[i] = CalculateAveragePayoff(option, context, priceMatrix, timeGrid, obsIdx);
        }

        return values;
    }

    public double[] Deltas(SnowballOption option, PricingContext context, double[] assetPrices)
    {
        double[] values = Values(option, context, assetPrices);

        using DisposeScope scope = torch.NewDisposeScope();

        torch.Tensor valueTensor = torch.tensor(values, torch.float64, Device);
        torch.Tensor deltaTensor = torch.zeros_like(valueTensor);

        double ds = assetPrices[1] - assetPrices[0];
        deltaTensor[1..^1] = (valueTensor[2..] - valueTensor[..^2]) / (2 * ds);
        deltaTensor[0] = (valueTensor[1] - valueTensor[0]) / ds;
        deltaTensor[^1] = (valueTensor[^1] - valueTensor[^2]) / ds;

        return deltaTensor.cpu().data<double>().ToArray();
    }

    public double[] Gammas(SnowballOption option, PricingContext context, double[] assetPrices)
    {
        double[] values = Values(option, context, assetPrices);

        using DisposeScope scope = torch.NewDisposeScope();

        torch.Tensor valueTensor = torch.tensor(values, torch.float64, Device);
        torch.Tensor gammaTensor = torch.zeros_like(valueTensor);

        double ds = assetPrices[1] - assetPrices[0];
        gammaTensor[1..^1] = (valueTensor[2..] - 2 * valueTensor[1..^1] + valueTensor[..^2]) / (ds * ds);
        gammaTensor[0] = (valueTensor[2] - 2 * valueTensor[1] + valueTensor[0]) / (ds * ds);
        gammaTensor[^1] = (valueTensor[^1] - 2 * valueTensor[^2] + valueTensor[^3]) / (ds * ds);

        return gammaTensor.cpu().data<double>().ToArray();
    }

    protected override double CalculateValue(SnowballOption option, PricingContext context)
    {
        if (option.BarrierTouchStatus == BarrierTouchStatus.UpTouch)
        {
            return 0.0;
        }

        using DisposeScope scope = torch.NewDisposeScope();

        (torch.Tensor timeGrid, torch.Tensor dtVector, torch.Tensor obsIdx, int stepCount) = PrepareSimulationParameters(option, context);

        if (stepCount <= 0)
        {
            return CalculateTerminalPayoff(option, context);
        }

        using RandomNumberSource source = new(pathCount, stepCount, Device);
        torch.Tensor priceMatrix = CreatePriceMatrix(context, dtVector, source);

        return CalculateAveragePayoff(option, context, priceMatrix, timeGrid, obsIdx);
    }

    private static torch.Tensor CreatePriceMatrix(PricingContext context, torch.Tensor dtVector, RandomNumberSource source) =>
        PathGenerator.Generate(context.AssetPrice, context.RiskFreeRate - context.DividendYield, context.Volatility, dtVector, source);

    private static double CalculateAveragePayoff(
        SnowballOption option,
        PricingContext context,
        torch.Tensor priceMatrix,
        torch.Tensor timeGrid,
        torch.Tensor obsIdx)
    {
        using DisposeScope scope = torch.NewDisposeScope();

        double r = context.RiskFreeRate;

        // Knock-out payoff calculation
        torch.Tensor obsPrices = priceMatrix.index_select(1, obsIdx);
        torch.Tensor koMatrix = obsPrices >= option.KnockOutPrice;
        torch.Tensor hasKnockedOut = koMatrix.any(1);
        torch.Tensor firstKoIdx = koMatrix.@long().argmax(1);
        torch.Tensor koStepIdx = obsIdx.index_select(0, firstKoIdx);

        torch.Tensor timeToKo = timeGrid.index_select(0, koStepIdx);
        double timeFromEffectiveToValuation = (context.ValuationDate.DayNumber - option.EffectiveDate.DayNumber) / 365.0;
        torch.Tensor couponAccrualTime = timeToKo + timeFromEffectiveToValuation;

        torch.Tensor discountedKoPayoff = option.KnockOutCouponRate * couponAccrualTime * torch.exp(-r * timeToKo);

        // Maturity payoff calculation
        torch.Tensor hasKnockedInOnFuturePath = (priceMatrix < option.KnockInPrice).any(1);
        torch.Tensor hasKnockedIn = option.BarrierTouchStatus == BarrierTouchStatus.DownTouch
            ? torch.ones_like(hasKnockedInOnFuturePath)
            : hasKnockedInOnFuturePath;

        double timeToMaturity = timeGrid[-1].item<double>();
        double dfFinal = Math.Exp(-r * timeToMaturity);
        double maturityCouponPayoff = option.MaturityCouponRate * (option.ExpirationDate.DayNumber - option.EffectiveDate.DayNumber) / 365.0;

        torch.Tensor finalSpot = priceMatrix.select(1, -1);
        torch.Tensor loss = torch.minimum(finalSpot - option.StrikePrice, 0);
        torch.Tensor discountedMaturityPayoff = torch.where(hasKnockedIn.logical_not(), maturityCouponPayoff, loss) * dfFinal;

        // Combine payoffs
        torch.Tensor pathPayoffs = torch.where(hasKnockedOut, discountedKoPayoff, discountedMaturityPayoff);

        return pathPayoffs.mean().item<double>();
    }

    private static double CalculateTerminalPayoff(SnowballOption option, PricingContext context)
    {
        Guard.IsEqualTo(context.ValuationDate, option.ExpirationDate);

        double t = (option.ExpirationDate.DayNumber - option.EffectiveDate.DayNumber) / 365.0;
        double maturityCouponPayoff = option.MaturityCouponRate * t;
        double loss = Math.Min(context.AssetPrice - option.StrikePrice, 0);

        if (context.AssetPrice >= option.KnockOutPrice)
        {
            return maturityCouponPayoff;
        }

        if (context.AssetPrice < option.KnockInPrice || option.BarrierTouchStatus == BarrierTouchStatus.DownTouch)
        {
            return loss;
        }

        return maturityCouponPayoff;
    }

    private (torch.Tensor TimeGrid, torch.Tensor DtVector, torch.Tensor ObsIdx, int StepCount)
        PrepareSimulationParameters(SnowballOption option, PricingContext context)
    {
        DateOnly valuationDate = context.ValuationDate;
        Guard.IsBetweenOrEqualTo(valuationDate, option.EffectiveDate, option.ExpirationDate);

        DateOnly[] futureTradingDays = DateUtils.GetTradingDays(valuationDate, option.ExpirationDate).ToArray();
        if (futureTradingDays.Length <= 1)
        {
            return (torch.empty(0, torch.float64, Device), torch.empty(0, torch.float64, Device), 0.0, 0);
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

        torch.Tensor timeGrid = torch.tensor(yearFractions, torch.float64, Device);
        torch.Tensor dtVector = torch.tensor(dtArray, torch.float64, Device);

        DateOnly[] futureObsDates = option.KnockOutObservationDates.Where(d => d >= valuationDate).ToArray();
        int[] obsIdxArray = new int[futureObsDates.Length];
        for (int i = 0; i < futureObsDates.Length; i++)
        {
            int index = Array.BinarySearch(futureTradingDays, futureObsDates[i]);
            if (index < 0)
            {
                ThrowHelper.ThrowArgumentException($"Observation date {futureObsDates[i]} is not a trading day.");
            }

            obsIdxArray[i] = index;
        }

        torch.Tensor obsIdx = torch.tensor(obsIdxArray, torch.int64, Device);

        return (timeGrid, dtVector, obsIdx, stepCount);
    }
}
