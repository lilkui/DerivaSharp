using CommunityToolkit.Diagnostics;
using CommunityToolkit.HighPerformance;
using DerivaSharp.Instruments;
using DerivaSharp.Models;
using DerivaSharp.Numerics;

namespace DerivaSharp.PricingEngines;

public sealed class FdPhoenixEngine(FiniteDifferenceScheme scheme, int priceStepCount, int timeStepCount)
    : FiniteDifference1DPricingEngine<PhoenixOption>(scheme, priceStepCount, timeStepCount)
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

    public double[] Values(PhoenixOption option, PricingContext<BsmModel> context, double[] assetPrices)
    {
        if (option.BarrierTouchStatus == BarrierTouchStatus.UpTouch)
        {
            return new double[assetPrices.Length];
        }

        int count = assetPrices.Length;
        Guard.IsGreaterThanOrEqualTo(count, 3);

        CalculateValue(option, context.Model, context.AssetPrice, context.ValuationDate);

        double[] values = new double[count];
        ReadOnlySpan<double> priceSpan = PriceVector;
        ReadOnlySpan<double> valueSpan = ValueMatrixSpan.GetRowSpan(0);

        for (int i = 0; i < count; i++)
        {
            values[i] = LinearInterpolation.InterpolateSorted(assetPrices[i], priceSpan, valueSpan);
        }

        return values;
    }

    public double[] Deltas(PhoenixOption option, PricingContext<BsmModel> context, double[] assetPrices)
    {
        double[] values = Values(option, context, assetPrices);
        double[] deltas = new double[values.Length];

        double ds = assetPrices[1] - assetPrices[0];

        for (int i = 1; i < values.Length - 1; i++)
        {
            deltas[i] = (values[i + 1] - values[i - 1]) / (2 * ds);
        }

        deltas[0] = (values[1] - values[0]) / ds;
        deltas[^1] = (values[^1] - values[^2]) / ds;

        return deltas;
    }

    public double[] Gammas(PhoenixOption option, PricingContext<BsmModel> context, double[] assetPrices)
    {
        double[] values = Values(option, context, assetPrices);
        double[] gammas = new double[values.Length];

        double ds = assetPrices[1] - assetPrices[0];

        for (int i = 1; i < values.Length - 1; i++)
        {
            gammas[i] = (values[i + 1] - 2 * values[i] + values[i - 1]) / (ds * ds);
        }

        gammas[0] = (values[2] - 2 * values[1] + values[0]) / (ds * ds);
        gammas[^1] = (values[^1] - 2 * values[^2] + values[^3]) / (ds * ds);

        return gammas;
    }

    protected override double CalculateValue(PhoenixOption option, BsmModel model, double assetPrice, DateOnly valuationDate)
    {
        if (option.BarrierTouchStatus == BarrierTouchStatus.UpTouch)
        {
            return 0.0;
        }

        PricingContext<BsmModel> context = new(model, assetPrice, valuationDate);
        InitializeParameters(option, context);

        if (!_hasDailyKnockIn)
        {
            _isSolvingKnockedIn = option.BarrierTouchStatus == BarrierTouchStatus.DownTouch;
            return base.CalculateValue(option, model, assetPrice, valuationDate);
        }

        if (option.BarrierTouchStatus == BarrierTouchStatus.DownTouch)
        {
            _isSolvingKnockedIn = true;
            return base.CalculateValue(option, model, assetPrice, valuationDate);
        }

        _isSolvingKnockedIn = true;
        base.CalculateValue(option, model, assetPrice, valuationDate);
        ValueMatrixSpan.CopyTo(_knockedInValues);

        _isSolvingKnockedIn = false;
        return base.CalculateValue(option, model, assetPrice, valuationDate);
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

    protected override void SetBoundaryConditions(PhoenixOption option, BsmModel model)
    {
        double r = model.RiskFreeRate;
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

    protected override void ApplyStepConditions(int i, PhoenixOption option, BsmModel model)
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

        if (!_isSolvingKnockedIn && _hasDailyKnockIn)
        {
            double kiPrice = option.KnockInPrice;
            ReadOnlySpan2D<double> knockedInSpan = new(_knockedInValues, TimeStepCount + 1, PriceStepCount + 1);

            for (int j = 0; j <= PriceStepCount; j++)
            {
                if (PriceVector[j] < kiPrice)
                {
                    ValueMatrixSpan[i, j] = knockedInSpan[i, j];
                }
            }
        }
    }

    private void InitializeParameters(PhoenixOption option, PricingContext<BsmModel> context)
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
