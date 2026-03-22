using CommunityToolkit.Diagnostics;
using DerivaSharp.Instruments;
using DerivaSharp.Models;
using DerivaSharp.Numerics;
using DerivaSharp.Time;

namespace DerivaSharp.PricingEngines;

/// <summary>
///     Base class for finite difference pricing engines for autocallable notes.
/// </summary>
/// <typeparam name="TOption">The type of autocallable note to price.</typeparam>
public abstract class FdAutocallableEngine<TOption>(FiniteDifferenceScheme scheme, int priceStepCount, int timeStepCount)
    : BsmFiniteDifferenceEngine<TOption>(scheme, priceStepCount, timeStepCount)
    where TOption : AutocallableNote
{
    /// <summary>
    ///     Gets the mapping from time step indices to observation indices (-1 if no observation).
    /// </summary>
    protected int[] StepToObservationIndex { get; private set; } = [];

    /// <summary>
    ///     Gets or sets the observation times in years from valuation date.
    /// </summary>
    protected double[]? ObservationTimes { get; set; }

    /// <inheritdoc/>
    protected override bool UseTradingDayGrid => true;

    /// <inheritdoc/>
    public override double[] Values(TOption option, in PricingContext<BsmModelParameters> context, double[] assetPrices)
    {
        if (IsUpTouched(option))
        {
            return new double[assetPrices.Length];
        }

        int count = assetPrices.Length;
        Guard.IsGreaterThanOrEqualTo(count, 3);

        CalculateValue(option, context);

        double[] values = new double[count];
        ReadOnlySpan<double> priceSpan = PriceVector;
        ReadOnlySpan<double> valueSpan = ValueMatrixSpan.GetRowSpan(0);

        for (int i = 0; i < count; i++)
        {
            values[i] = LinearInterpolation.InterpolateSorted(assetPrices[i], priceSpan, valueSpan);
        }

        return values;
    }

    /// <inheritdoc/>
    protected override double CalculateValue(TOption option, in PricingContext<BsmModelParameters> context)
    {
        if (IsUpTouched(option))
        {
            return 0.0;
        }

        InitializeParameters(option, context.ValuationDate, context.Calendar);
        return SolveGrid(option, context);
    }

    /// <summary>
    ///     Solves the PDE grid. The default implementation delegates to the base FD solver.
    ///     Override in derived classes to implement multi-pass algorithms (e.g., knock-in two-pass).
    /// </summary>
    protected virtual double SolveGrid(TOption option, in PricingContext<BsmModelParameters> context)
    {
        return base.CalculateValue(option, context);
    }

    /// <summary>
    ///     Determines whether the option has already knocked out before valuation.
    /// </summary>
    protected abstract bool IsUpTouched(TOption option);

    /// <summary>
    ///     Initializes product-specific parameters from the option and valuation context.
    /// </summary>
    protected abstract void InitializeParameters(TOption option, DateOnly valuationDate, ICalendar calendar);

    /// <inheritdoc/>
    protected override void InitializeGrid(TOption option, BsmModelParameters parameters, DateOnly valuationDate)
    {
        if (ObservationTimes is null)
        {
            InitializeParameters(option, valuationDate, Calendar);
        }

        base.InitializeGrid(option, parameters, valuationDate);

        if (StepToObservationIndex.Length != TimeStepCount + 1)
        {
            StepToObservationIndex = new int[TimeStepCount + 1];
        }

        MapObservationSteps(ObservationTimes!, StepToObservationIndex);
    }
}
