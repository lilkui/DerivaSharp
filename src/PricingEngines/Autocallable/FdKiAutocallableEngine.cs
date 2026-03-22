using CommunityToolkit.HighPerformance;
using DerivaSharp.Instruments;
using DerivaSharp.Models;
using DerivaSharp.Time;

namespace DerivaSharp.PricingEngines;

/// <summary>
///     Base class for finite difference pricing engines for knock-in autocallable notes.
///     Implements the two-pass algorithm: first solving the knocked-in state, then
///     the not-knocked-in state with knock-in substitution at the barrier.
/// </summary>
/// <typeparam name="TOption">The type of knock-in autocallable note to price.</typeparam>
public abstract class FdKiAutocallableEngine<TOption>(FiniteDifferenceScheme scheme, int priceStepCount, int timeStepCount)
    : FdAutocallableEngine<TOption>(scheme, priceStepCount, timeStepCount)
    where TOption : KiAutocallableNote
{
    private double[] _knockedInValues = [];
    private bool[] _stepToKnockInObservation = [];
    private double[]? _knockInTimes;

    /// <summary>
    ///     Gets a value that indicates whether the current solve pass is for the knocked-in state.
    /// </summary>
    protected bool IsSolvingKnockedIn { get; private set; }

    /// <inheritdoc/>
    protected override bool IsUpTouched(TOption option) =>
        option.BarrierTouchStatus == BarrierTouchStatus.UpTouch;

    /// <inheritdoc/>
    protected override double SolveGrid(TOption option, in PricingContext<BsmModelParameters> context)
    {
        if (!RequiresTwoPass(option))
        {
            IsSolvingKnockedIn = option.BarrierTouchStatus == BarrierTouchStatus.DownTouch;
            return base.SolveGrid(option, context);
        }

        if (option.BarrierTouchStatus == BarrierTouchStatus.DownTouch)
        {
            IsSolvingKnockedIn = true;
            return base.SolveGrid(option, context);
        }

        // Pass 1: solve the knocked-in scenario and store the resulting value surface.
        IsSolvingKnockedIn = true;
        base.SolveGrid(option, context);
        ValueMatrixSpan.CopyTo(_knockedInValues);

        // Pass 2: solve the not-knocked-in scenario, applying the knock-in substitution.
        IsSolvingKnockedIn = false;
        return base.SolveGrid(option, context);
    }

    /// <summary>
    ///     Determines whether this option requires the two-pass solve. The default is <see langword="true" />.
    ///     Override to return <see langword="false" /> when a single pass suffices (e.g., at-expiry knock-in).
    /// </summary>
    /// <param name="option">The knock-in autocallable note to check.</param>
    /// <returns><see langword="true" /> if the two-pass algorithm is required; otherwise, <see langword="false" />.</returns>
    protected virtual bool RequiresTwoPass(TOption option) => true;

    /// <inheritdoc/>
    protected override void InitializeGrid(TOption option, BsmModelParameters parameters, DateOnly valuationDate)
    {
        base.InitializeGrid(option, parameters, valuationDate);

        if (_stepToKnockInObservation.Length != TimeStepCount + 1)
        {
            _stepToKnockInObservation = new bool[TimeStepCount + 1];
        }

        int requiredKnockedInValues = (TimeStepCount + 1) * (PriceStepCount + 1);
        if (_knockedInValues.Length != requiredKnockedInValues)
        {
            _knockedInValues = new double[requiredKnockedInValues];
        }

        if (option.KnockInObservationFrequency == ObservationFrequency.Daily && _knockInTimes is not null)
        {
            MapObservationFlags(_knockInTimes, _stepToKnockInObservation);
        }
        else
        {
            Array.Clear(_stepToKnockInObservation);
        }
    }

    /// <summary>
    ///     Computes knock-in observation times from trading days.
    /// </summary>
    /// <param name="option">The knock-in autocallable note being priced.</param>
    /// <param name="valuationDate">The date as of which the option is valued.</param>
    /// <param name="calendar">The trading calendar used to determine business days.</param>
    protected void ComputeKnockInTimes(TOption option, DateOnly valuationDate, ICalendar calendar)
    {
        if (option.KnockInObservationFrequency == ObservationFrequency.Daily)
        {
            DateOnly[] tradingDays = calendar.GetTradingDays(valuationDate, option.ExpirationDate).ToArray();
            _knockInTimes = new double[tradingDays.Length];
            for (int i = 0; i < tradingDays.Length; i++)
            {
                _knockInTimes[i] = (tradingDays[i].DayNumber - valuationDate.DayNumber) / 365.0;
            }
        }
        else
        {
            _knockInTimes = null;
        }
    }

    /// <summary>
    ///     Applies knock-in substitution at the given time step if the conditions are met.
    /// </summary>
    /// <param name="i">The time step index.</param>
    /// <param name="option">The knock-in autocallable note being priced.</param>
    protected void ApplyKnockInSubstitutionIfNeeded(int i, TOption option)
    {
        bool apply = !IsSolvingKnockedIn
                     && option.KnockInObservationFrequency == ObservationFrequency.Daily
                     && _stepToKnockInObservation[i];
        ApplyKnockInSubstitution(i, option.KnockInPrice, apply, _knockedInValues);
    }

    /// <summary>
    ///     Applies knock-in substitution by replacing values below the knock-in price with knocked-in values.
    /// </summary>
    /// <param name="i">The time step index.</param>
    /// <param name="knockInPrice">The knock-in barrier price.</param>
    /// <param name="apply"><see langword="true" /> to apply the knock-in substitution; otherwise, <see langword="false" />.</param>
    /// <param name="knockedInValues">The values to use when knocked in.</param>
    private void ApplyKnockInSubstitution(int i, double knockInPrice, bool apply, double[] knockedInValues)
    {
        if (!apply)
        {
            return;
        }

        ReadOnlySpan2D<double> knockInMatrix = new(knockedInValues, TimeStepCount + 1, PriceStepCount + 1);
        for (int j = 0; j <= PriceStepCount; j++)
        {
            if (PriceVector[j] < knockInPrice)
            {
                ValueMatrixSpan[i, j] = knockInMatrix[i, j];
            }
        }
    }
}
