using CommunityToolkit.Diagnostics;
using DerivaSharp.Instruments;
using DerivaSharp.Models;

namespace DerivaSharp.PricingEngines;

public sealed class FdPhoenixEngine(FiniteDifferenceScheme scheme, int priceStepCount, int timeStepCount)
    : FdKiAutocallableEngine<PhoenixOption>(scheme, priceStepCount, timeStepCount)
{
    private readonly double[] _knockedInValues = new double[(timeStepCount + 1) * (priceStepCount + 1)];
    private readonly int[] _stepToObservationIndex = new int[timeStepCount + 1];
    private bool _isSolvingKnockedIn;
    private bool _hasDailyKnockIn;
    private double[]? _observationTimes;
    private double[]? _observationPrices;
    private double[]? _couponBarriers;
    private double _couponAmount;
    private double _lossAtZero;

    protected override double CalculateValue(PhoenixOption option, BsmModelParameters parameters, double assetPrice, DateOnly valuationDate)
    {
        if (option.BarrierTouchStatus == BarrierTouchStatus.UpTouch)
        {
            return 0.0;
        }

        PricingContext<BsmModelParameters> context = new(parameters, assetPrice, valuationDate);
        InitializeParameters(option, context);

        if (!_hasDailyKnockIn)
        {
            _isSolvingKnockedIn = option.BarrierTouchStatus == BarrierTouchStatus.DownTouch;
            return base.CalculateValue(option, parameters, assetPrice, valuationDate);
        }

        if (option.BarrierTouchStatus == BarrierTouchStatus.DownTouch)
        {
            _isSolvingKnockedIn = true;
            return base.CalculateValue(option, parameters, assetPrice, valuationDate);
        }

        _isSolvingKnockedIn = true;
        base.CalculateValue(option, parameters, assetPrice, valuationDate);
        ValueMatrixSpan.CopyTo(_knockedInValues);

        _isSolvingKnockedIn = false;
        return base.CalculateValue(option, parameters, assetPrice, valuationDate);
    }

    protected override void SetTerminalCondition(PhoenixOption option)
    {
        double upperStrike = option.UpperStrikePrice;
        double lowerStrike = option.LowerStrikePrice;
        double initialPrice = option.InitialPrice;
        double knockInPrice = option.KnockInPrice;
        bool atExpiryKnockIn = option.KnockInObservationFrequency == ObservationFrequency.AtExpiry;

        for (int j = 0; j <= PriceStepCount; j++)
        {
            double s = PriceVector[j];
            double loss = Math.Clamp(s - upperStrike, lowerStrike - upperStrike, 0) / initialPrice;

            if (_isSolvingKnockedIn)
            {
                ValueMatrixSpan[TimeStepCount, j] = loss;
            }
            else if (atExpiryKnockIn)
            {
                ValueMatrixSpan[TimeStepCount, j] = s < knockInPrice || option.BarrierTouchStatus == BarrierTouchStatus.DownTouch
                    ? loss
                    : 0.0;
            }
            else
            {
                ValueMatrixSpan[TimeStepCount, j] = 0.0;
            }
        }
    }

    protected override void SetBoundaryConditions(PhoenixOption option, BsmModelParameters parameters)
    {
        double r = parameters.RiskFreeRate;
        double maturity = TimeVector[TimeStepCount];

        int nextObsIdx = 0;
        int nObs = _observationTimes!.Length;

        for (int i = 0; i <= TimeStepCount; i++)
        {
            double t = TimeVector[i];
            double df = Math.Exp(-r * (maturity - t));

            ValueMatrixSpan[i, 0] = _lossAtZero * df;

            while (nextObsIdx < nObs && _observationTimes[nextObsIdx] < t - 1e-6)
            {
                nextObsIdx++;
            }

            if (nextObsIdx < nObs)
            {
                double obsTime = _observationTimes[nextObsIdx];
                ValueMatrixSpan[i, PriceStepCount] = _couponAmount * Math.Exp(-r * (obsTime - t));
            }
            else
            {
                ValueMatrixSpan[i, PriceStepCount] = 0.0;
            }
        }
    }

    protected override void ApplyStepConditions(int i, PhoenixOption option, BsmModelParameters parameters)
    {
        int obsIdx = _stepToObservationIndex[i];
        if (obsIdx != -1)
        {
            double koPrice = _observationPrices![obsIdx];
            double couponBarrier = _couponBarriers![obsIdx];

            for (int j = 0; j <= PriceStepCount; j++)
            {
                double s = PriceVector[j];
                bool hitsKo = s >= koPrice;
                bool hitsCoupon = s >= couponBarrier;

                if (hitsKo)
                {
                    ValueMatrixSpan[i, j] = hitsCoupon ? _couponAmount : 0.0;
                }
                else if (hitsCoupon)
                {
                    ValueMatrixSpan[i, j] += _couponAmount;
                }
            }
        }

        ApplyKnockInSubstitution(i, option.KnockInPrice, !_isSolvingKnockedIn && _hasDailyKnockIn, _knockedInValues);
    }

    private void InitializeParameters(PhoenixOption option, PricingContext<BsmModelParameters> context)
    {
        DateOnly valDate = context.ValuationDate;
        DateOnly[] obsDates = option.KnockOutObservationDates;
        int n = obsDates.Length;

        Guard.IsGreaterThan(n, 0);
        Guard.IsEqualTo(option.CouponBarrierPrices.Length, n);
        Guard.IsEqualTo(option.KnockOutPrices.Length, n);

        _observationTimes = new double[n];
        _observationPrices = option.KnockOutPrices;
        _couponBarriers = option.CouponBarrierPrices;
        _couponAmount = option.InitialPrice * option.CouponRate;
        _hasDailyKnockIn = option.KnockInObservationFrequency == ObservationFrequency.Daily;

        for (int i = 0; i < n; i++)
        {
            _observationTimes[i] = (obsDates[i].DayNumber - valDate.DayNumber) / 365.0;
        }

        _lossAtZero = (option.LowerStrikePrice - option.UpperStrikePrice) / option.InitialPrice;

        MinPrice = 0.0;
        double maxBarrier = Math.Max(option.InitialPrice, Math.Max(_observationPrices.Max(), _couponBarriers.Max()));
        MaxPrice = maxBarrier * 4.0;

        double tMax = (option.ExpirationDate.DayNumber - valDate.DayNumber) / 365.0;
        MapObservationSteps(_observationTimes, _stepToObservationIndex, tMax);
    }
}
