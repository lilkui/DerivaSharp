using DerivaSharp.Instruments;
using DerivaSharp.Models;
using DerivaSharp.Time;

namespace DerivaSharp.PricingEngines;

/// <summary>
///     Finite difference pricing engine for ternary snowball options.
/// </summary>
public sealed class FdTernarySnowballEngine(FiniteDifferenceScheme scheme, int priceStepCount, int timeStepCount)
    : FdKiAutocallableEngine<TernarySnowballOption>(scheme, priceStepCount, timeStepCount)
{
    private double[]? _observationPrices;
    private double[]? _observationCoupons;
    private double[]? _observationAccruedTimes;
    private double _principalPayoff;
    private double _maturityPayoff;
    private double _minimalPayoff;

    protected override void InitializeParameters(TernarySnowballOption option, DateOnly valuationDate, ICalendar calendar)
    {
        DateOnly effDate = option.EffectiveDate;
        DateOnly[] obsDates = option.KnockOutObservationDates;
        int n = obsDates.Length;

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
        _minimalPayoff = _principalPayoff + option.MinimalCouponRate * maturityTime;

        MinPrice = 0.0;
        double maxBarrier = n > 0 ? option.KnockOutPrices.Max() : option.InitialPrice;
        MaxPrice = Math.Max(option.InitialPrice, maxBarrier) * 4.0;

        ComputeKnockInTimes(option, valuationDate, calendar);
    }

    protected override void SetTerminalCondition(TernarySnowballOption option)
    {
        double knockInPrice = option.KnockInPrice;

        for (int j = 0; j <= PriceStepCount; j++)
        {
            double s = PriceVector[j];

            if (IsSolvingKnockedIn || s < knockInPrice)
            {
                ValueMatrixSpan[TimeStepCount, j] = _minimalPayoff;
            }
            else
            {
                ValueMatrixSpan[TimeStepCount, j] = _maturityPayoff;
            }
        }
    }

    protected override void SetBoundaryConditions(TernarySnowballOption option, BsmModelParameters parameters)
    {
        double r = parameters.RiskFreeRate;
        double maturity = TimeVector[TimeStepCount];

        int nextObsIdx = 0;
        int nObs = ObservationTimes!.Length;

        for (int i = 0; i <= TimeStepCount; i++)
        {
            double t = TimeVector[i];
            double df = Math.Exp(-r * (maturity - t));

            ValueMatrixSpan[i, 0] = _minimalPayoff * df;

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
                double terminalPayoff = IsSolvingKnockedIn ? _minimalPayoff : _maturityPayoff;
                ValueMatrixSpan[i, PriceStepCount] = terminalPayoff * df;
            }
        }
    }

    protected override void ApplyStepConditions(int i, TernarySnowballOption option, BsmModelParameters parameters)
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
