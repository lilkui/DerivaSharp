using CommunityToolkit.Diagnostics;
using DerivaSharp.Instruments;
using DerivaSharp.Models;
using DerivaSharp.Time;

namespace DerivaSharp.PricingEngines;

/// <summary>
///     Finite difference pricing engine for Phoenix autocallable options.
/// </summary>
public sealed class FdPhoenixEngine(FiniteDifferenceScheme scheme, int priceStepCount, int timeStepCount)
    : FdKiAutocallableEngine<PhoenixOption>(scheme, priceStepCount, timeStepCount)
{
    private double[]? _observationPrices;
    private double[]? _couponBarriers;
    private double _couponAmount;
    private double _lossAtZero;

    protected override bool RequiresTwoPass(PhoenixOption option) =>
        option.KnockInObservationFrequency == ObservationFrequency.Daily;

    protected override void InitializeParameters(PhoenixOption option, DateOnly valuationDate, ICalendar calendar)
    {
        DateOnly[] obsDates = option.KnockOutObservationDates;
        int n = obsDates.Length;

        Guard.IsGreaterThan(n, 0);
        Guard.IsEqualTo(option.CouponBarrierPrices.Length, n);
        Guard.IsEqualTo(option.KnockOutPrices.Length, n);

        ObservationTimes = new double[n];
        _observationPrices = option.KnockOutPrices;
        _couponBarriers = option.CouponBarrierPrices;
        _couponAmount = option.InitialPrice * option.CouponRate;

        for (int i = 0; i < n; i++)
        {
            ObservationTimes[i] = (obsDates[i].DayNumber - valuationDate.DayNumber) / 365.0;
        }

        _lossAtZero = (option.LowerStrikePrice - option.UpperStrikePrice) / option.InitialPrice;

        MinPrice = 0.0;
        double maxBarrier = Math.Max(option.InitialPrice, Math.Max(_observationPrices.Max(), _couponBarriers.Max()));
        MaxPrice = maxBarrier * 4.0;

        ComputeKnockInTimes(option, valuationDate, calendar);
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

            if (IsSolvingKnockedIn)
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
        int obsIdx = StepToObservationIndex[i];
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

        ApplyKnockInSubstitutionIfNeeded(i, option);
    }
}
