using CommunityToolkit.Diagnostics;
using DerivaSharp.Instruments;
using DerivaSharp.Models;
using DerivaSharp.Time;
using TorchSharp;
using Tensor = TorchSharp.torch.Tensor;

namespace DerivaSharp.PricingEngines;

public sealed class McSnowballEngine(int pathCount, bool useCuda = false) : BsmPricingEngine<SnowballOption>
{
    private readonly torch.Device _device = TorchUtils.GetDevice(useCuda);

    public double[] Values(SnowballOption option, PricingContext<BsmModelParameters> context, double[] assetPrices)
    {
        if (option.BarrierTouchStatus == BarrierTouchStatus.UpTouch)
        {
            return new double[assetPrices.Length];
        }

        int count = assetPrices.Length;
        Guard.IsGreaterThanOrEqualTo(count, 3);

        using DisposeScope scope = torch.NewDisposeScope();

        SimulationData simData = PrepareSimulationData(option, context);

        if (simData.StepCount <= 0)
        {
            return assetPrices.Select(s => CalculateTerminalPayoff(option, context with { AssetPrice = s })).ToArray();
        }

        using RandomNumberSource source = new(pathCount, simData.StepCount, _device);

        double[] values = new double[count];
        for (int i = 0; i < count; i++)
        {
            PricingContext<BsmModelParameters> assetContext = context with { AssetPrice = assetPrices[i] };
            using Tensor priceMatrix = CreatePriceMatrix(assetContext, simData.DtVector, source);
            values[i] = CalculateAveragePayoff(option, assetContext, priceMatrix, simData);
        }

        return values;
    }

    public double[] Deltas(SnowballOption option, PricingContext<BsmModelParameters> context, double[] assetPrices)
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

    public double[] Gammas(SnowballOption option, PricingContext<BsmModelParameters> context, double[] assetPrices)
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

    protected override double CalculateValue(SnowballOption option, BsmModelParameters parameters, double assetPrice, DateOnly valuationDate)
    {
        if (option.BarrierTouchStatus == BarrierTouchStatus.UpTouch)
        {
            return 0.0;
        }

        PricingContext<BsmModelParameters> context = new(parameters, assetPrice, valuationDate);
        using DisposeScope scope = torch.NewDisposeScope();

        SimulationData simData = PrepareSimulationData(option, context);

        if (simData.StepCount <= 0)
        {
            return CalculateTerminalPayoff(option, context);
        }

        using RandomNumberSource source = new(pathCount, simData.StepCount, _device);
        Tensor priceMatrix = CreatePriceMatrix(context, simData.DtVector, source);

        return CalculateAveragePayoff(option, context, priceMatrix, simData);
    }

    private static Tensor CreatePriceMatrix(PricingContext<BsmModelParameters> context, Tensor dtVector, RandomNumberSource source)
    {
        BsmModelParameters parameters = context.ModelParameters;
        return PathGenerator.Generate(context.AssetPrice, parameters.RiskFreeRate - parameters.DividendYield, parameters.Volatility, dtVector, source);
    }

    private static double CalculateAveragePayoff(
        SnowballOption option,
        PricingContext<BsmModelParameters> context,
        Tensor priceMatrix,
        in SimulationData simData)
    {
        using DisposeScope scope = torch.NewDisposeScope();

        BsmModelParameters parameters = context.ModelParameters;
        double r = parameters.RiskFreeRate;

        // Knock-out payoff calculation
        Tensor obsPrices = priceMatrix.index_select(1, simData.ObsIdx);
        Tensor koPriceRow = simData.KoPrices.unsqueeze(0);
        Tensor koMatrix = obsPrices >= koPriceRow;
        Tensor hasKnockedOut = koMatrix.any(1);
        Tensor firstKoIdx = koMatrix.@long().argmax(1);
        Tensor koStepIdx = simData.ObsIdx.index_select(0, firstKoIdx);

        Tensor timeToKo = simData.TimeGrid.index_select(0, koStepIdx);
        double timeFromEffectiveToValuation = (context.ValuationDate.DayNumber - option.EffectiveDate.DayNumber) / 365.0;
        Tensor couponAccrualTime = timeToKo + timeFromEffectiveToValuation;

        Tensor pathKoCouponRate = simData.KoCouponRates.index_select(0, firstKoIdx);
        Tensor discountedKoPayoff = pathKoCouponRate * couponAccrualTime * torch.exp(-r * timeToKo);

        // Maturity payoff calculation
        Tensor finalSpot = priceMatrix.select(1, -1);
        Tensor hasKnockedInOnFuturePath = option.KnockInObservationFrequency == ObservationFrequency.AtExpiry
            ? finalSpot < option.KnockInPrice
            : (priceMatrix < option.KnockInPrice).any(1);
        Tensor hasKnockedIn = option.BarrierTouchStatus == BarrierTouchStatus.DownTouch
            ? torch.ones_like(hasKnockedInOnFuturePath)
            : hasKnockedInOnFuturePath;

        double timeToMaturity = simData.TimeGrid[-1].item<double>();
        double dfFinal = Math.Exp(-r * timeToMaturity);
        double maturityCouponPayoff = option.MaturityCouponRate * (option.ExpirationDate.DayNumber - option.EffectiveDate.DayNumber) / 365.0;

        Tensor loss = torch.clamp_(finalSpot - option.UpperStrikePrice, option.LowerStrikePrice - option.UpperStrikePrice, 0).div_(option.InitialPrice);
        Tensor discountedMaturityPayoff = torch.where(hasKnockedIn.logical_not(), maturityCouponPayoff, loss) * dfFinal;

        // Combine payoffs
        Tensor pathPayoffs = torch.where(hasKnockedOut, discountedKoPayoff, discountedMaturityPayoff);

        return pathPayoffs.mean().item<double>();
    }

    private static double CalculateTerminalPayoff(SnowballOption option, PricingContext<BsmModelParameters> context)
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

    private SimulationData PrepareSimulationData(SnowballOption option, PricingContext<BsmModelParameters> context)
    {
        DateOnly valuationDate = context.ValuationDate;
        Guard.IsBetweenOrEqualTo(valuationDate, option.EffectiveDate, option.ExpirationDate);

        DateOnly[] futureTradingDays = DateUtils.GetTradingDays(valuationDate, option.ExpirationDate).ToArray();
        if (futureTradingDays.Length <= 1)
        {
            return new SimulationData(
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
            int index = futureTradingDays.AsSpan().BinarySearch(futureObsDates[i]);
            if (index < 0)
            {
                ThrowHelper.ThrowInvalidOperationException($"Observation date {futureObsDates[i]} is not a trading day.");
            }

            obsIdxArray[i] = index;
            (double price, double rate) = koMap[futureObsDates[i]];
            koPricesArray[i] = price;
            koCouponRatesArray[i] = rate;
        }

        Tensor obsIdx = torch.tensor(obsIdxArray, torch.int64, _device);
        Tensor koPrices = torch.tensor(koPricesArray, torch.float64, _device);
        Tensor koCouponRates = torch.tensor(koCouponRatesArray, torch.float64, _device);

        return new SimulationData(timeGrid, dtVector, obsIdx, koPrices, koCouponRates, stepCount);
    }

    private readonly record struct SimulationData(Tensor TimeGrid, Tensor DtVector, Tensor ObsIdx, Tensor KoPrices, Tensor KoCouponRates, int StepCount);
}
