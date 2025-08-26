using CommunityToolkit.Diagnostics;
using DerivaSharp.Instruments;
using DerivaSharp.Time;
using TorchSharp;
using Tensor = TorchSharp.torch.Tensor;

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

        SimulationParameters parameters = PrepareSimulationParameters(option, context);

        if (parameters.StepCount <= 0)
        {
            return assetPrices.Select(s => CalculateTerminalPayoff(option, context with { AssetPrice = s })).ToArray();
        }

        using RandomNumberSource source = new(pathCount, parameters.StepCount, Device);

        double[] values = new double[count];
        for (int i = 0; i < count; i++)
        {
            using Tensor priceMatrix = CreatePriceMatrix(context with { AssetPrice = assetPrices[i] }, parameters.DtVector, source);
            values[i] = CalculateAveragePayoff(option, context, priceMatrix, parameters);
        }

        return values;
    }

    public double[] Deltas(SnowballOption option, PricingContext context, double[] assetPrices)
    {
        double[] values = Values(option, context, assetPrices);

        using DisposeScope scope = torch.NewDisposeScope();

        Tensor valueTensor = torch.tensor(values, torch.float64, Device);
        Tensor deltaTensor = torch.zeros_like(valueTensor);

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

        Tensor valueTensor = torch.tensor(values, torch.float64, Device);
        Tensor gammaTensor = torch.zeros_like(valueTensor);

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

        SimulationParameters parameters = PrepareSimulationParameters(option, context);

        if (parameters.StepCount <= 0)
        {
            return CalculateTerminalPayoff(option, context);
        }

        using RandomNumberSource source = new(pathCount, parameters.StepCount, Device);
        Tensor priceMatrix = CreatePriceMatrix(context, parameters.DtVector, source);

        return CalculateAveragePayoff(option, context, priceMatrix, parameters);
    }

    private static Tensor CreatePriceMatrix(PricingContext context, Tensor dtVector, RandomNumberSource source) =>
        PathGenerator.Generate(context.AssetPrice, context.RiskFreeRate - context.DividendYield, context.Volatility, dtVector, source);

    private static double CalculateAveragePayoff(
        SnowballOption option,
        PricingContext context,
        Tensor priceMatrix,
        in SimulationParameters parameters)
    {
        using DisposeScope scope = torch.NewDisposeScope();

        double r = context.RiskFreeRate;

        // Knock-out payoff calculation
        Tensor obsPrices = priceMatrix.index_select(1, parameters.ObsIdx);
        Tensor koPriceRow = parameters.KoPrices.unsqueeze(0);
        Tensor koMatrix = obsPrices >= koPriceRow;
        Tensor hasKnockedOut = koMatrix.any(1);
        Tensor firstKoIdx = koMatrix.@long().argmax(1);
        Tensor koStepIdx = parameters.ObsIdx.index_select(0, firstKoIdx);

        Tensor timeToKo = parameters.TimeGrid.index_select(0, koStepIdx);
        double timeFromEffectiveToValuation = (context.ValuationDate.DayNumber - option.EffectiveDate.DayNumber) / 365.0;
        Tensor couponAccrualTime = timeToKo + timeFromEffectiveToValuation;

        Tensor pathKoCouponRate = parameters.KoCouponRates.index_select(0, firstKoIdx);
        Tensor discountedKoPayoff = pathKoCouponRate * couponAccrualTime * torch.exp(-r * timeToKo);

        // Maturity payoff calculation
        Tensor hasKnockedInOnFuturePath = (priceMatrix < option.KnockInPrice).any(1);
        Tensor hasKnockedIn = option.BarrierTouchStatus == BarrierTouchStatus.DownTouch
            ? torch.ones_like(hasKnockedInOnFuturePath)
            : hasKnockedInOnFuturePath;

        double timeToMaturity = parameters.TimeGrid[-1].item<double>();
        double dfFinal = Math.Exp(-r * timeToMaturity);
        double maturityCouponPayoff = option.MaturityCouponRate * (option.ExpirationDate.DayNumber - option.EffectiveDate.DayNumber) / 365.0;

        Tensor finalSpot = priceMatrix.select(1, -1);
        Tensor loss = torch.minimum(finalSpot - option.StrikePrice, 0).div_(option.InitialPrice);
        Tensor discountedMaturityPayoff = torch.where(hasKnockedIn.logical_not(), maturityCouponPayoff, loss) * dfFinal;

        // Combine payoffs
        Tensor pathPayoffs = torch.where(hasKnockedOut, discountedKoPayoff, discountedMaturityPayoff);

        return pathPayoffs.mean().item<double>();
    }

    private static double CalculateTerminalPayoff(SnowballOption option, PricingContext context)
    {
        Guard.IsEqualTo(context.ValuationDate, option.ExpirationDate);

        double t = (option.ExpirationDate.DayNumber - option.EffectiveDate.DayNumber) / 365.0;
        double loss = Math.Min(context.AssetPrice - option.StrikePrice, 0) / option.InitialPrice;

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

    private SimulationParameters PrepareSimulationParameters(SnowballOption option, PricingContext context)
    {
        DateOnly valuationDate = context.ValuationDate;
        Guard.IsBetweenOrEqualTo(valuationDate, option.EffectiveDate, option.ExpirationDate);

        DateOnly[] futureTradingDays = DateUtils.GetTradingDays(valuationDate, option.ExpirationDate).ToArray();
        if (futureTradingDays.Length <= 1)
        {
            return new SimulationParameters(
                torch.empty(0, torch.float64, Device),
                torch.empty(0, torch.float64, Device),
                torch.empty(0, torch.int64, Device),
                torch.empty(0, torch.float64, Device),
                torch.empty(0, torch.float64, Device),
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

        Tensor timeGrid = torch.tensor(yearFractions, torch.float64, Device);
        Tensor dtVector = torch.tensor(dtArray, torch.float64, Device);

        DateOnly[] futureObsDates = option.KnockOutObservationDates.Where(d => d >= valuationDate).ToArray();

        Dictionary<DateOnly, (double, double)> koMap = new(option.KnockOutObservationDates.Length);
        for (int i = 0; i < option.KnockOutObservationDates.Length; i++)
        {
            koMap[option.KnockOutObservationDates[i]] = (option.KnockOutPrices[i], option.KnockOutCouponRates[i]);
        }

        int n = futureObsDates.Length;
        int[] obsIdxArray = new int[n];
        double[] koPricesArray = new double[n];
        double[] koCouponRatesArray = new double[n];

        for (int i = 0; i < futureObsDates.Length; i++)
        {
            int index = Array.BinarySearch(futureTradingDays, futureObsDates[i]);
            if (index < 0)
            {
                ThrowHelper.ThrowArgumentException($"Observation date {futureObsDates[i]} is not a trading day.");
            }

            obsIdxArray[i] = index;
            (double price, double rate) = koMap[futureObsDates[i]];
            koPricesArray[i] = price;
            koCouponRatesArray[i] = rate;
        }

        Tensor obsIdx = torch.tensor(obsIdxArray, torch.int64, Device);
        Tensor koPrices = torch.tensor(koPricesArray, torch.float64, Device);
        Tensor koCouponRates = torch.tensor(koCouponRatesArray, torch.float64, Device);

        return new SimulationParameters(timeGrid, dtVector, obsIdx, koPrices, koCouponRates, stepCount);
    }

    private readonly record struct SimulationParameters(Tensor TimeGrid, Tensor DtVector, Tensor ObsIdx, Tensor KoPrices, Tensor KoCouponRates, int StepCount);
}
