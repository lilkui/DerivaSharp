using DerivaSharp.Instruments;
using DerivaSharp.Models;
using DerivaSharp.Time;

namespace DerivaSharp.PricingEngines;

public sealed class FdTernarySnowballEngine(FiniteDifferenceScheme scheme, int priceStepCount, int timeStepCount)
    : FdKiAutocallableEngine<TernarySnowballOption>(scheme, priceStepCount, timeStepCount)
{
    private double[] _knockedInValues = [];
    private int[] _stepToObservationIndex = [];
    private bool[] _stepToKnockInObservation = [];
    private bool _isSolvingKnockedIn;
    private double[]? _observationTimes;
    private double[]? _knockInTimes;
    private double[]? _observationPrices;
    private double[]? _observationCoupons;
    private double[]? _observationAccruedTimes;
    private double _maturityPayoff;
    private double _minimalPayoff;

    protected override double CalculateValue(TernarySnowballOption option, BsmModelParameters parameters, double assetPrice, DateOnly valuationDate)
    {
        if (option.BarrierTouchStatus == BarrierTouchStatus.UpTouch)
        {
            return 0.0;
        }

        InitializeParameters(option, valuationDate);

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

    protected override void SetTerminalCondition(TernarySnowballOption option)
    {
        double knockInPrice = option.KnockInPrice;

        for (int j = 0; j <= PriceStepCount; j++)
        {
            double s = PriceVector[j];

            if (_isSolvingKnockedIn || s < knockInPrice)
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
        int nObs = _observationTimes!.Length;

        for (int i = 0; i <= TimeStepCount; i++)
        {
            double t = TimeVector[i];
            double df = Math.Exp(-r * (maturity - t));

            ValueMatrixSpan[i, 0] = _minimalPayoff * df;

            while (nextObsIdx < nObs && _observationTimes[nextObsIdx] < t - 1e-6)
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
                double terminalPayoff = _isSolvingKnockedIn ? _minimalPayoff : _maturityPayoff;
                ValueMatrixSpan[i, PriceStepCount] = terminalPayoff * df;
            }
        }
    }

    protected override void ApplyStepConditions(int i, TernarySnowballOption option, BsmModelParameters parameters)
    {
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

        bool applyKnockIn = !_isSolvingKnockedIn
                            && option.KnockInObservationFrequency == ObservationFrequency.Daily
                            && _stepToKnockInObservation[i];
        ApplyKnockInSubstitution(i, option.KnockInPrice, applyKnockIn, _knockedInValues);
    }

    protected override void InitializeCoefficients(TernarySnowballOption option, BsmModelParameters parameters, DateOnly valuationDate)
    {
        if (_observationTimes is null)
        {
            InitializeParameters(option, valuationDate);
        }

        base.InitializeCoefficients(option, parameters, valuationDate);

        if (_stepToObservationIndex.Length != TimeStepCount + 1)
        {
            _stepToObservationIndex = new int[TimeStepCount + 1];
        }

        if (_stepToKnockInObservation.Length != TimeStepCount + 1)
        {
            _stepToKnockInObservation = new bool[TimeStepCount + 1];
        }

        int requiredKnockedInValues = (TimeStepCount + 1) * (PriceStepCount + 1);
        if (_knockedInValues.Length != requiredKnockedInValues)
        {
            _knockedInValues = new double[requiredKnockedInValues];
        }

        MapObservationSteps(_observationTimes!, _stepToObservationIndex);

        if (option.KnockInObservationFrequency == ObservationFrequency.Daily && _knockInTimes is not null)
        {
            MapObservationFlags(_knockInTimes, _stepToKnockInObservation);
        }
        else
        {
            Array.Clear(_stepToKnockInObservation);
        }
    }

    private void InitializeParameters(TernarySnowballOption option, DateOnly valuationDate)
    {
        DateOnly effDate = option.EffectiveDate;
        DateOnly[] obsDates = option.KnockOutObservationDates;
        int n = obsDates.Length;

        _observationTimes = new double[n];
        _observationAccruedTimes = new double[n];
        _observationPrices = option.KnockOutPrices;
        _observationCoupons = option.KnockOutCouponRates;

        for (int i = 0; i < n; i++)
        {
            _observationTimes[i] = (obsDates[i].DayNumber - valuationDate.DayNumber) / 365.0;
            _observationAccruedTimes[i] = (obsDates[i].DayNumber - effDate.DayNumber) / 365.0;
        }

        double maturityTime = (option.ExpirationDate.DayNumber - effDate.DayNumber) / 365.0;
        _maturityPayoff = option.MaturityCouponRate * maturityTime;
        _minimalPayoff = option.MinimalCouponRate * maturityTime;

        MinPrice = 0.0;
        double maxBarrier = n > 0 ? option.KnockOutPrices.Max() : option.InitialPrice;
        MaxPrice = Math.Max(option.InitialPrice, maxBarrier) * 4.0;

        if (option.KnockInObservationFrequency == ObservationFrequency.Daily)
        {
            DateOnly[] tradingDays = DateUtils.GetTradingDays(valuationDate, option.ExpirationDate).ToArray();
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
}
