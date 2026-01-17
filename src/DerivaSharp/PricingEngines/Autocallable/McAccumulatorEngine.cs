using CommunityToolkit.Diagnostics;
using DerivaSharp.Instruments;
using DerivaSharp.Models;
using TorchSharp;
using ZLinq;
using Tensor = TorchSharp.torch.Tensor;

namespace DerivaSharp.PricingEngines;

public sealed class McAccumulatorEngine(int pathCount, bool useCuda = false) : BsmPricingEngine<Accumulator>
{
    private readonly torch.Device _device = TorchUtils.GetDevice(useCuda);

    public override double[] Values(Accumulator option, PricingContext<BsmModelParameters> context, ReadOnlySpan<double> assetPrices)
    {
        int count = assetPrices.Length;
        Guard.IsGreaterThanOrEqualTo(count, 3);

        using DisposeScope scope = torch.NewDisposeScope();

        SimulationData simData = PrepareSimulationData(option, context);

        if (simData.StepCount <= 0)
        {
            return assetPrices.AsValueEnumerable().Select(s => CalculateTerminalPayoff(option, context with { AssetPrice = s })).ToArray();
        }

        using RandomNumberSource source = new(pathCount, simData.StepCount, _device);

        double[] values = new double[count];
        for (int i = 0; i < count; i++)
        {
            PricingContext<BsmModelParameters> assetContext = context with { AssetPrice = assetPrices[i] };
            using Tensor priceMatrix = PathGenerator.Generate(assetContext, simData.DtVector, source);
            values[i] = CalculateAveragePayoff(option, assetContext, priceMatrix, simData);
        }

        return values;
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
        Tensor priceMatrix = PathGenerator.Generate(context, simData.DtVector, source);

        return CalculateAveragePayoff(option, context, priceMatrix, simData);
    }

    private static double CalculateAveragePayoff(
        Accumulator option,
        PricingContext<BsmModelParameters> context,
        Tensor priceMatrix,
        in SimulationData simData)
    {
        using DisposeScope scope = torch.NewDisposeScope();

        double r = context.ModelParameters.RiskFreeRate;

        Tensor koHit = priceMatrix >= option.KnockOutPrice;
        Tensor hasKo = koHit.any(1);
        Tensor firstKoIdx = koHit.@long().argmax(1);

        Tensor obsIndices = torch.arange(priceMatrix.size(1), torch.int64, priceMatrix.device).unsqueeze(0);
        Tensor activeMask = torch.where(hasKo.unsqueeze(1), obsIndices < firstKoIdx.unsqueeze(1), torch.ones_like(koHit));
        Tensor accumulateMask = activeMask.logical_and(priceMatrix < option.KnockOutPrice);

        Tensor baseQty = torch.full_like(priceMatrix, option.DailyQuantity);
        Tensor accelQty = baseQty * option.AccelerationFactor;
        Tensor qtyPerDay = torch.where(priceMatrix < option.StrikePrice, accelQty, baseQty);
        Tensor effectiveQty = torch.where(accumulateMask, qtyPerDay, torch.zeros_like(qtyPerDay));

        Tensor totalQuantity = effectiveQty.sum(1).add(option.AccumulatedQuantity);

        Tensor koSpot = priceMatrix.gather(1, firstKoIdx.unsqueeze(1)).squeeze(1);
        Tensor finalSpot = priceMatrix.select(1, -1);
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

        TradingDayGrid grid = TradingDayGridBuilder.Build(valuationDate, option.ExpirationDate, _device);
        return new SimulationData(grid.TimeGrid, grid.DtVector, grid.StepCount);
    }

    private readonly record struct SimulationData(Tensor TimeGrid, Tensor DtVector, int StepCount);
}
