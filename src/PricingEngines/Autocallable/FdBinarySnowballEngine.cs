using DerivaSharp.Instruments;
using DerivaSharp.Models;
using DerivaSharp.Time;

namespace DerivaSharp.PricingEngines;

/// <summary>
///     Finite difference pricing engine for binary snowball options.
/// </summary>
public sealed class FdBinarySnowballEngine(FiniteDifferenceScheme scheme, int priceStepCount, int timeStepCount)
    : FdAutocallableEngine<BinarySnowballOption>(scheme, priceStepCount, timeStepCount)
{
    private IReadOnlyList<double>? _observationPrices;
    private IReadOnlyList<double>? _observationCoupons;
    private double[]? _observationAccruedTimes;
    private double _principalPayoff;
    private double _maturityPayoff;

    protected override bool IsUpTouched(BinarySnowballOption option) =>
        option.BarrierTouchStatus == BarrierTouchStatus.UpTouch;

    protected override void InitializeParameters(BinarySnowballOption option, DateOnly valuationDate, ICalendar calendar)
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

        MinPrice = 0.0;
        double maxBarrier = n > 0 ? option.KnockOutPrices.Max() : option.InitialPrice;
        MaxPrice = Math.Max(option.InitialPrice, maxBarrier) * 4.0;
    }

    protected override void SetTerminalCondition(BinarySnowballOption option)
    {
        for (int j = 0; j <= PriceStepCount; j++)
        {
            ValueMatrixSpan[TimeStepCount, j] = _maturityPayoff;
        }
    }

    protected override void SetBoundaryConditions(BinarySnowballOption option, BsmModelParameters parameters)
    {
        double r = parameters.RiskFreeRate;
        double maturity = TimeVector[TimeStepCount];

        int nextObsIdx = 0;
        int nObs = ObservationTimes!.Length;

        for (int i = 0; i <= TimeStepCount; i++)
        {
            double t = TimeVector[i];
            double df = Math.Exp(-r * (maturity - t));

            ValueMatrixSpan[i, 0] = _maturityPayoff * df;

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
                ValueMatrixSpan[i, PriceStepCount] = _maturityPayoff * df;
            }
        }
    }

    protected override void ApplyStepConditions(int i, BinarySnowballOption option, BsmModelParameters parameters)
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
    }
}
