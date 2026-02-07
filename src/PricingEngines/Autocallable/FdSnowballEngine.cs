using DerivaSharp.Instruments;
using DerivaSharp.Models;
using DerivaSharp.Time;

namespace DerivaSharp.PricingEngines;

// A Snowball has two economic “states”: (1) not-knocked-in yet, and (2) already knocked-in.
// Once knock-in has occurred, future cashflows no longer depend on the pre-knock-in path;
// the contract from that time onward is fully described by (t, S) under the “knocked-in” rules.
// That makes the valuation Markov in (t, S, KI-flag), and the PDE/FD backward induction can be
// done separately per state and then linked by a state-switch condition at the knock-in barrier.
//
// Pass 1 (knocked-in surface):
//   Solve the FD grid assuming KI-flag = true from the start. This produces V_KI(t, S) on the
//   entire (time, price) grid, with terminal/boundary/step conditions consistent with being
//   knocked in (but still applying knock-out at observation dates). Store this surface.
//
// Pass 2 (not-knocked-in surface):
//   Solve again with KI-flag = false. At each time step, apply knock-out step conditions as usual.
//   Then, wherever the price process triggers knock-in (e.g., daily monitoring and S < KI barrier),
//   “switch regimes” by substituting the continuation value with the precomputed knocked-in value:
//       V_NKI(t, S) := V_KI(t, S) on the knock-in region.
//   This enforces the correct dynamic programming condition: immediately after knock-in, the option’s
//   value must equal the value of the same contract in the knocked-in state at the same (t, S).
//
// In other words, the two passes implement a coupled two-state PDE by solving the KI state first
// and using it as a boundary/interface condition for the NKI state.
public sealed class FdSnowballEngine(FiniteDifferenceScheme scheme, int priceStepCount, int timeStepCount)
    : FdKiAutocallableEngine<SnowballOption>(scheme, priceStepCount, timeStepCount)
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
    private double _lossAtZero;

    protected override double CalculateValue(SnowballOption option, BsmModelParameters parameters, double assetPrice, DateOnly valuationDate)
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

        // First pass: solve the knocked-in scenario and store the resulting value surface.
        _isSolvingKnockedIn = true;
        base.CalculateValue(option, parameters, assetPrice, valuationDate);

        ValueMatrixSpan.CopyTo(_knockedInValues);

        // Second pass: solve the not-knocked-in scenario, applying the knock-in substitution.
        _isSolvingKnockedIn = false;
        return base.CalculateValue(option, parameters, assetPrice, valuationDate);
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

    protected override void SetBoundaryConditions(SnowballOption option, BsmModelParameters parameters)
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
                double coupon = _observationCoupons![nextObsIdx] * _observationAccruedTimes![nextObsIdx];
                ValueMatrixSpan[i, PriceStepCount] = coupon * Math.Exp(-r * (obsTime - t));
            }
            else
            {
                double terminalPayoff = _isSolvingKnockedIn ? 0.0 : _maturityPayoff;
                ValueMatrixSpan[i, PriceStepCount] = terminalPayoff * df;
            }
        }
    }

    protected override void ApplyStepConditions(int i, SnowballOption option, BsmModelParameters parameters)
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

    protected override void InitializeCoefficients(SnowballOption option, BsmModelParameters parameters, DateOnly valuationDate)
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

    private void InitializeParameters(SnowballOption option, DateOnly valuationDate)
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
        _lossAtZero = (option.LowerStrikePrice - option.UpperStrikePrice) / option.InitialPrice;

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

        double[] events = option.KnockInObservationFrequency == ObservationFrequency.Daily && _knockInTimes is not null
            ? MergeEventTimes(_observationTimes, _knockInTimes)
            : _observationTimes;
        SetEventTimes(events);
    }
}
