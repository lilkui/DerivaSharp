using CommunityToolkit.Diagnostics;
using DerivaSharp.Instruments;
using DerivaSharp.Models;
using TorchSharp;
using Tensor = TorchSharp.torch.Tensor;

namespace DerivaSharp.PricingEngines;

/// <summary>
///     Base class for Monte Carlo pricing engines of autocallable notes.
/// </summary>
/// <typeparam name="TOption">The type of autocallable option.</typeparam>
/// <param name="pathCount">The number of simulation paths.</param>
/// <param name="useCuda">Whether to use CUDA for GPU acceleration.</param>
/// <param name="seed">The optional random seed used to make generated samples deterministic.</param>
public abstract class McAutocallableEngine<TOption>(int pathCount, bool useCuda = false, int? seed = null) : BsmPricingEngine<TOption>
    where TOption : AutocallableNote
{
    /// <summary>
    ///     Gets the Torch device used for tensor allocations in this engine.
    /// </summary>
    protected torch.Device Device { get; } = TorchUtils.GetDevice(useCuda);

    public override double[] Values(TOption option, in PricingContext<BsmModelParameters> context, double[] assetPrices)
    {
        if (IsUpTouched(option))
        {
            return new double[assetPrices.Length];
        }

        int count = assetPrices.Length;
        Guard.IsGreaterThanOrEqualTo(count, 3);

        using DisposeScope scope = torch.NewDisposeScope();

        SimulationData simData = PrepareSimulationData(option, context);
        if (simData.StepCount <= 0)
        {
            PricingContext<BsmModelParameters> ctx = context;
            return assetPrices.Select(s => CalculateTerminalPayoff(option, ctx with { AssetPrice = s })).ToArray();
        }

        using RandomNumberSource source = new(pathCount, simData.StepCount, Device, seed);

        double[] values = new double[count];
        for (int i = 0; i < count; i++)
        {
            PricingContext<BsmModelParameters> assetContext = context with { AssetPrice = assetPrices[i] };
            using Tensor priceMatrix = PathGenerator.Generate(assetContext, simData.DtVector, source);
            values[i] = CalculateAveragePayoff(option, assetContext, priceMatrix, simData);
        }

        return values;
    }

    /// <summary>
    ///     Builds knock-out state tensors shared by derived payoff implementations.
    /// </summary>
    /// <param name="priceMatrix">Simulated paths with shape [pathCount, stepCount].</param>
    /// <param name="simData">Precomputed simulation data and observation indices.</param>
    /// <returns>
    ///     A <see cref="KnockOutState" /> containing observation prices, knock-out mask, first knock-out indices,
    ///     and corresponding knock-out times.
    /// </returns>
    protected static KnockOutState BuildKnockOutState(Tensor priceMatrix, in SimulationData simData)
    {
        Tensor obsPrices = priceMatrix.index_select(1, simData.ObsIdx);
        Tensor koPriceRow = simData.KoPrices.unsqueeze(0);
        Tensor koMatrix = obsPrices >= koPriceRow;
        Tensor hasKnockedOut = koMatrix.any(1);
        Tensor firstKoIdx = koMatrix.@long().argmax(1);
        Tensor koStepIdx = simData.ObsIdx.index_select(0, firstKoIdx);
        Tensor timeToKo = simData.TimeGrid.index_select(0, koStepIdx);

        return new KnockOutState(obsPrices, koMatrix, hasKnockedOut, firstKoIdx, koStepIdx, timeToKo);
    }

    protected override double CalculateValue(TOption option, in PricingContext<BsmModelParameters> context)
    {
        if (IsUpTouched(option))
        {
            return 0.0;
        }

        using DisposeScope scope = torch.NewDisposeScope();

        SimulationData simData = PrepareSimulationData(option, context);
        if (simData.StepCount <= 0)
        {
            return CalculateTerminalPayoff(option, context);
        }

        using RandomNumberSource source = new(pathCount, simData.StepCount, Device, seed);
        Tensor priceMatrix = PathGenerator.Generate(context, simData.DtVector, source);

        return CalculateAveragePayoff(option, context, priceMatrix, simData);
    }

    /// <summary>
    ///     Determines whether the option has already knocked out before valuation.
    /// </summary>
    /// <param name="option">The option being priced.</param>
    /// <returns><see langword="true" /> when pricing should short-circuit to zero; otherwise <see langword="false" />.</returns>
    protected abstract bool IsUpTouched(TOption option);

    /// <summary>
    ///     Builds option-specific observation payload aligned with future knock-out observation points.
    /// </summary>
    /// <param name="option">The option being priced.</param>
    /// <param name="futureScheduleIndices">
    ///     Indices into the original observation schedule for dates that are greater than or equal to valuation date.
    /// </param>
    /// <returns>
    ///     A 1-D tensor used by derived payoff logic (for example, coupon rates or coupon barriers), with one value
    ///     per future observation date and aligned with <see cref="SimulationData.ObsIdx" />.
    /// </returns>
    protected abstract Tensor BuildObservationAuxTensor(TOption option, ReadOnlySpan<int> futureScheduleIndices);

    /// <summary>
    ///     Calculates the Monte Carlo payoff estimate when path simulation is required.
    /// </summary>
    /// <param name="option">The option being priced.</param>
    /// <param name="context">Pricing context for the current valuation.</param>
    /// <param name="priceMatrix">Simulated paths with shape [pathCount, stepCount].</param>
    /// <param name="simData">Precomputed simulation data and observation tensors.</param>
    /// <returns>The discounted expected payoff across simulated paths.</returns>
    protected abstract double CalculateAveragePayoff(
        TOption option,
        in PricingContext<BsmModelParameters> context,
        Tensor priceMatrix,
        in SimulationData simData);

    /// <summary>
    ///     Calculates the terminal payoff when valuation is at expiration or no future steps are simulated.
    /// </summary>
    /// <param name="option">The option being priced.</param>
    /// <param name="context">Pricing context for the current valuation.</param>
    /// <returns>The terminal payoff at the current asset level.</returns>
    protected abstract double CalculateTerminalPayoff(TOption option, in PricingContext<BsmModelParameters> context);

    private SimulationData PrepareSimulationData(TOption option, in PricingContext<BsmModelParameters> context)
    {
        DateOnly valuationDate = context.ValuationDate;
        Guard.IsBetweenOrEqualTo(valuationDate, option.EffectiveDate, option.ExpirationDate);

        TradingDayGrid grid = TradingDayGridBuilder.Build(valuationDate, option.ExpirationDate, context.Calendar, Device);
        if (grid.StepCount <= 0)
        {
            return new SimulationData(
                grid.TimeGrid,
                grid.DtVector,
                torch.empty(0, torch.int64, Device),
                torch.empty(0, torch.float64, Device),
                torch.empty(0, torch.float64, Device),
                0);
        }

        IReadOnlyList<DateOnly> observationDates = option.KnockOutObservationDates;
        int futureCount = 0;
        for (int i = 0; i < observationDates.Count; i++)
        {
            if (observationDates[i] >= valuationDate)
            {
                futureCount++;
            }
        }

        DateOnly[] futureObsDates = new DateOnly[futureCount];
        int[] futureScheduleIndices = new int[futureCount];
        int cursor = 0;
        for (int i = 0; i < observationDates.Count; i++)
        {
            if (observationDates[i] < valuationDate)
            {
                continue;
            }

            futureObsDates[cursor] = observationDates[i];
            futureScheduleIndices[cursor] = i;
            cursor++;
        }

        int[] obsIdxArray = new int[futureCount];
        double[] koPricesArray = new double[futureCount];
        for (int i = 0; i < futureCount; i++)
        {
            int index = grid.TradingDays.AsSpan().BinarySearch(futureObsDates[i]);
            if (index < 0)
            {
                ThrowHelper.ThrowInvalidOperationException($"Observation date {futureObsDates[i]} is not a trading day.");
            }

            obsIdxArray[i] = index;
            koPricesArray[i] = option.KnockOutPrices[futureScheduleIndices[i]];
        }

        Tensor obsIdx = torch.tensor(obsIdxArray, torch.int64, Device);
        Tensor koPrices = torch.tensor(koPricesArray, torch.float64, Device);
        Tensor obsAux = BuildObservationAuxTensor(option, futureScheduleIndices);

        return new SimulationData(grid.TimeGrid, grid.DtVector, obsIdx, koPrices, obsAux, grid.StepCount);
    }

    /// <summary>
    ///     Shared tensors used by the Monte Carlo autocallable pricing workflow.
    /// </summary>
    protected readonly record struct SimulationData(Tensor TimeGrid, Tensor DtVector, Tensor ObsIdx, Tensor KoPrices, Tensor ObsAux, int StepCount);

    /// <summary>
    ///     Knock-out state tensors derived from simulated paths and observation schedules.
    /// </summary>
    protected readonly record struct KnockOutState(Tensor ObsPrices, Tensor KoMatrix, Tensor HasKnockedOut, Tensor FirstKoIdx, Tensor KoStepIdx, Tensor TimeToKo);
}
