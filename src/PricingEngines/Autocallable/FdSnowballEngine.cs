using DerivaSharp.Instruments;
using DerivaSharp.Models;
using DerivaSharp.Time;

namespace DerivaSharp.PricingEngines;

/// <summary>
///     Pricing engine for snowball options using finite difference methods with knock-in and knock-out features.
/// </summary>
/// <param name="scheme">One of the <see cref="FiniteDifferenceScheme" /> enumeration values that specifies the scheme to use.</param>
/// <param name="priceStepCount">The number of price steps in the grid.</param>
/// <param name="timeStepCount">The number of time steps in the grid.</param>
public sealed class FdSnowballEngine(FiniteDifferenceScheme scheme, int priceStepCount, int timeStepCount)
    : FdKiAutocallableEngine<SnowballOption>(scheme, priceStepCount, timeStepCount)
{
    private IReadOnlyList<double>? _observationPrices;
    private IReadOnlyList<double>? _observationCoupons;
    private double[]? _observationAccruedTimes;
    private double _principalPayoff;
    private double _maturityPayoff;
    private double _lossAtZero;

    /// <inheritdoc/>
    protected override void InitializeParameters(SnowballOption option, DateOnly valuationDate, ICalendar calendar)
    {
        DateOnly effDate = option.EffectiveDate;
        IReadOnlyList<DateOnly> obsDates = option.KnockOutObservationDates;
        int n = obsDates.Count;

        ObservationTimes = new double[n];
        _observationAccruedTimes = new double[n];
        _observationPrices = option.KnockOutPrices;
        _observationCoupons = option.KnockOutCouponRates;

        for (int i = 0; i < n; i++)
        {
            ObservationTimes[i] = (obsDates[i].DayNumber - valuationDate.DayNumber) / 365.0;
            _observationAccruedTimes[i] = (obsDates[i].DayNumber - effDate.DayNumber) / 365.0;
        }

        double maturityTime = (option.ExpirationDate.DayNumber - effDate.DayNumber) / 365.0;
        _principalPayoff = option.PrincipalRatio;
        _maturityPayoff = _principalPayoff + option.MaturityCouponRate * maturityTime;
        _lossAtZero = _principalPayoff + (option.LowerStrikePrice - option.UpperStrikePrice) / option.InitialPrice;

        MinPrice = 0.0;
        double maxBarrier = n > 0 ? option.KnockOutPrices.Max() : option.InitialPrice;
        MaxPrice = Math.Max(option.InitialPrice, maxBarrier) * 4.0;

        ComputeKnockInTimes(option, valuationDate, calendar);
    }

    /// <inheritdoc/>
    protected override void SetTerminalCondition(SnowballOption option)
    {
        double upperStrike = option.UpperStrikePrice;
        double lowerStrike = option.LowerStrikePrice;
        double initialPrice = option.InitialPrice;
        double knockInPrice = option.KnockInPrice;

        for (int j = 0; j <= PriceStepCount; j++)
        {
            double s = PriceVector[j];
            double loss = _principalPayoff + Math.Clamp(s - upperStrike, lowerStrike - upperStrike, 0) / initialPrice;

            if (IsSolvingKnockedIn || s < knockInPrice)
            {
                ValueMatrixSpan[TimeStepCount, j] = loss;
            }
            else
            {
                ValueMatrixSpan[TimeStepCount, j] = _maturityPayoff;
            }
        }
    }

    /// <inheritdoc/>
    protected override void SetBoundaryConditions(SnowballOption option, BsmModelParameters parameters)
    {
        double r = parameters.RiskFreeRate;
        double maturity = TimeVector[TimeStepCount];

        int nextObsIdx = 0;
        int nObs = ObservationTimes!.Length;

        for (int i = 0; i <= TimeStepCount; i++)
        {
            double t = TimeVector[i];
            double df = Math.Exp(-r * (maturity - t));

            ValueMatrixSpan[i, 0] = _lossAtZero * df;

            while (nextObsIdx < nObs && ObservationTimes[nextObsIdx] < t - 1e-6)
            {
                nextObsIdx++;
            }

            if (nextObsIdx < nObs)
            {
                double obsTime = ObservationTimes[nextObsIdx];
                double coupon = _principalPayoff + _observationCoupons![nextObsIdx] * _observationAccruedTimes![nextObsIdx];
                ValueMatrixSpan[i, PriceStepCount] = coupon * Math.Exp(-r * (obsTime - t));
            }
            else
            {
                double terminalPayoff = IsSolvingKnockedIn ? _principalPayoff : _maturityPayoff;
                ValueMatrixSpan[i, PriceStepCount] = terminalPayoff * df;
            }
        }
    }

    /// <inheritdoc/>
    protected override void ApplyStepConditions(int i, SnowballOption option, BsmModelParameters parameters)
    {
        int obsIdx = StepToObservationIndex[i];
        if (obsIdx != -1)
        {
            double koPrice = _observationPrices![obsIdx];
            double payoff = _principalPayoff + _observationCoupons![obsIdx] * _observationAccruedTimes![obsIdx];

            for (int j = 0; j <= PriceStepCount; j++)
            {
                if (PriceVector[j] >= koPrice)
                {
                    ValueMatrixSpan[i, j] = payoff;
                }
            }
        }

        ApplyKnockInSubstitutionIfNeeded(i, option);
    }
}
