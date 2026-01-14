using CommunityToolkit.Diagnostics;
using DerivaSharp.Instruments;
using DerivaSharp.Models;
using DerivaSharp.Time;
using TorchSharp;
using Tensor = TorchSharp.torch.Tensor;

namespace DerivaSharp.PricingEngines;

public sealed class McAccumulatorEngine(int pathCount, bool useCuda = false) : BsmPricingEngine<Accumulator>
{
    private readonly torch.Device _device = TorchUtils.GetDevice(useCuda);

    public double[] Values(Accumulator option, PricingContext<BsmModelParameters> context, double[] assetPrices)
    {
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

    public double[] Deltas(Accumulator option, PricingContext<BsmModelParameters> context, double[] assetPrices)
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

    public double[] Gammas(Accumulator option, PricingContext<BsmModelParameters> context, double[] assetPrices)
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

    protected override double CalculateValue(Accumulator option, BsmModelParameters parameters, double assetPrice, DateOnly valuationDate)
    {
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
        Accumulator option,
        PricingContext<BsmModelParameters> context,
        Tensor priceMatrix,
        in SimulationData simData)
    {
        using DisposeScope scope = torch.NewDisposeScope();

        double r = context.ModelParameters.RiskFreeRate;

        Tensor obsPrices = priceMatrix;
        Tensor koHit = obsPrices >= option.KnockOutPrice;
        Tensor hasKo = koHit.any(1);
        Tensor firstKoIdx = koHit.@long().argmax(1);

        Tensor obsIndices = torch.arange(obsPrices.size(1), torch.int64, obsPrices.device).unsqueeze(0);
        Tensor activeMask = torch.where(hasKo.unsqueeze(1), obsIndices < firstKoIdx.unsqueeze(1), torch.ones_like(koHit));
        Tensor accumulateMask = activeMask.logical_and(obsPrices < option.KnockOutPrice);

        Tensor baseQty = torch.full_like(obsPrices, option.DailyQuantity);
        Tensor accelQty = baseQty * option.AccelerationFactor;
        Tensor qtyPerDay = torch.where(obsPrices < option.StrikePrice, accelQty, baseQty);
        Tensor effectiveQty = torch.where(accumulateMask, qtyPerDay, torch.zeros_like(qtyPerDay));

        Tensor totalQuantity = effectiveQty.sum(1).add(option.AccumulatedQuantity);

        Tensor koSpot = obsPrices.gather(1, firstKoIdx.unsqueeze(1)).squeeze(1);
        Tensor finalSpot = obsPrices.select(1, -1);
        Tensor terminalSpot = torch.where(hasKo, koSpot, finalSpot);

        Tensor koTime = simData.TimeGrid.index_select(0, firstKoIdx);
        Tensor maturityTime = torch.full_like(koTime, simData.TimeGrid[-1].item<double>());
        Tensor terminalTime = torch.where(hasKo, koTime, maturityTime);

        Tensor payoff = totalQuantity * (terminalSpot - option.StrikePrice);
        Tensor discounted = payoff * torch.exp(-r * terminalTime);

        return discounted.mean().item<double>();
    }

    private static double CalculateTerminalPayoff(Accumulator option, PricingContext<BsmModelParameters> context)
    {
        Guard.IsEqualTo(context.ValuationDate, option.ExpirationDate);

        return option.AccumulatedQuantity * (context.AssetPrice - option.StrikePrice);
    }

    private SimulationData PrepareSimulationData(Accumulator option, PricingContext<BsmModelParameters> context)
    {
        DateOnly valuationDate = context.ValuationDate;
        Guard.IsBetweenOrEqualTo(valuationDate, option.EffectiveDate, option.ExpirationDate);

        DateOnly[] futureTradingDays = DateUtils.GetTradingDays(valuationDate, option.ExpirationDate).ToArray();
        if (futureTradingDays.Length <= 1)
        {
            return new SimulationData(
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

        return new SimulationData(timeGrid, dtVector, stepCount);
    }

    private readonly record struct SimulationData(Tensor TimeGrid, Tensor DtVector, int StepCount);
}
