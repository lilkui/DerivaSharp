using CommunityToolkit.HighPerformance;
using DerivaSharp.Instruments;

namespace DerivaSharp.PricingEngines;

public sealed class FdSnowballEngine(FiniteDifferenceScheme scheme, int priceStepCount, int timeStepCount)
    : FiniteDifferencePricingEngine<SnowballOption>(scheme, priceStepCount, timeStepCount)
{
    private readonly double[] _knockedInValues = new double[(timeStepCount + 1) * (priceStepCount + 1)];
    private readonly int[] _stepToObservationIndex = new int[timeStepCount + 1];
    private bool _isSolvingKnockedIn;
    private double[]? _observationTimes;
    private double[]? _observationPrices;
    private double[]? _observationCoupons;
    private double[]? _observationAccruedTimes;
    private double _maturityPayoff;
    private double _lossAtZero;

    protected override double CalculateValue(SnowballOption option, PricingContext context)
    {
        if (option.BarrierTouchStatus == BarrierTouchStatus.UpTouch)
        {
            return 0.0;
        }

        InitializeParameters(option, context);

        if (option.BarrierTouchStatus == BarrierTouchStatus.DownTouch)
        {
            _isSolvingKnockedIn = true;
            return base.CalculateValue(option, context);
        }

        // First pass: calculate value assuming the barrier has already been breached (knocked-in).
        _isSolvingKnockedIn = true;
        base.CalculateValue(option, context);

        ValueMatrixSpan.CopyTo(_knockedInValues);

        // Second pass: calculate value assuming the barrier has NOT been breached yet.
        // If the price hits the barrier during this pass, we switch to the value from the first pass.
        _isSolvingKnockedIn = false;
        return base.CalculateValue(option, context);
    }

    protected override void SetTerminalCondition(SnowballOption option)
    {
        double upperStrike = option.UpperStrikePrice;
        double lowerStrike = option.LowerStrikePrice;
        double initialPrice = option.InitialPrice;
        double knockInPrice = option.KnockInPrice;

        for (int j = 0; j <= PriceStepCount; j++)
        {
            double s = PriceVector[j];
            double loss = Math.Clamp(s - upperStrike, lowerStrike - upperStrike, 0) / initialPrice;

            if (_isSolvingKnockedIn || s < knockInPrice)
            {
                ValueMatrixSpan[TimeStepCount, j] = loss;
            }
            else
            {
                ValueMatrixSpan[TimeStepCount, j] = _maturityPayoff;
            }
        }
    }

    protected override void SetBoundaryConditions(SnowballOption option, PricingContext context)
    {
        double r = context.RiskFreeRate;
        double maturity = TimeVector[TimeStepCount];

        int nextObsIdx = 0;
        int nObs = _observationTimes!.Length;

        for (int i = 0; i <= TimeStepCount; i++)
        {
            double t = TimeVector[i];
            double df = Math.Exp(-r * (maturity - t));

            ValueMatrixSpan[i, 0] = _lossAtZero * df;

            if (_isSolvingKnockedIn)
            {
                ValueMatrixSpan[i, PriceStepCount] = 0.0;
            }
            else
            {
                while (nextObsIdx < nObs && _observationTimes[nextObsIdx] <= t + 1e-6)
                {
                    nextObsIdx++;
                }

                if (nextObsIdx < nObs)
                {
                    double obsTime = _observationTimes[nextObsIdx];
                    double coupon = _observationCoupons![nextObsIdx] * _observationAccruedTimes![nextObsIdx];
                    ValueMatrixSpan[i, PriceStepCount] = coupon * Math.Exp(-r * (obsTime - t));
                }
                else
                {
                    ValueMatrixSpan[i, PriceStepCount] = _maturityPayoff * df;
                }
            }
        }
    }

    protected override void ApplyStepConditions(int i, SnowballOption option, PricingContext context)
    {
        // Check for knock-out event at observation dates.
        int obsIdx = _stepToObservationIndex[i];
        if (obsIdx != -1)
        {
            double koPrice = _observationPrices![obsIdx];
            double payoff = _observationCoupons![obsIdx] * _observationAccruedTimes![obsIdx];

            for (int j = 0; j <= PriceStepCount; j++)
            {
                if (PriceVector[j] >= koPrice)
                {
                    ValueMatrixSpan[i, j] = payoff;
                }
            }
        }

        // Check for knock-in event: if price drops below barrier, use the pre-calculated knock-in value.
        if (!_isSolvingKnockedIn)
        {
            double kiPrice = option.KnockInPrice;
            ReadOnlySpan2D<double> knockedInSpan = _knockedInValues.AsSpan2D(TimeStepCount + 1, PriceStepCount + 1);
            for (int j = 0; j <= PriceStepCount; j++)
            {
                if (PriceVector[j] < kiPrice)
                {
                    ValueMatrixSpan[i, j] = knockedInSpan[i, j];
                }
            }
        }
    }

    private void InitializeParameters(SnowballOption option, PricingContext context)
    {
        DateOnly valDate = context.ValuationDate;
        DateOnly effDate = option.EffectiveDate;
        DateOnly[] obsDates = option.KnockOutObservationDates;
        int n = obsDates.Length;

        _observationTimes = new double[n];
        _observationAccruedTimes = new double[n];
        _observationPrices = option.KnockOutPrices;
        _observationCoupons = option.KnockOutCouponRates;

        for (int i = 0; i < n; i++)
        {
            _observationTimes[i] = (obsDates[i].DayNumber - valDate.DayNumber) / 365.0;
            _observationAccruedTimes[i] = (obsDates[i].DayNumber - effDate.DayNumber) / 365.0;
        }

        double maturityTime = (option.ExpirationDate.DayNumber - effDate.DayNumber) / 365.0;
        _maturityPayoff = option.MaturityCouponRate * maturityTime;
        _lossAtZero = (option.LowerStrikePrice - option.UpperStrikePrice) / option.InitialPrice;

        MinPrice = 0.0;
        double maxBarrier = n > 0 ? option.KnockOutPrices.Max() : option.InitialPrice;
        MaxPrice = Math.Max(option.InitialPrice * 3.0, maxBarrier * 1.5);

        double tMax = (option.ExpirationDate.DayNumber - valDate.DayNumber) / 365.0;
        double dt = tMax / TimeStepCount;
        _stepToObservationIndex.AsSpan().Fill(-1);

        for (int k = 0; k < n; k++)
        {
            double tObs = _observationTimes[k];
            int step = (int)Math.Round(tObs / dt);
            if (step >= 0 && step <= TimeStepCount && Math.Abs(step * dt - tObs) < dt / 2.0)
            {
                _stepToObservationIndex[step] = k;
            }
        }
    }
}
