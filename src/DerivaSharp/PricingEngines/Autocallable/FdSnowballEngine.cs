using CommunityToolkit.Diagnostics;
using CommunityToolkit.HighPerformance;
using DerivaSharp.Instruments;
using DerivaSharp.Models;
using DerivaSharp.Numerics;

namespace DerivaSharp.PricingEngines;

public sealed class FdSnowballEngine(FiniteDifferenceScheme scheme, int priceStepCount, int timeStepCount)
    : FiniteDifference1DPricingEngine<SnowballOption>(scheme, priceStepCount, timeStepCount)
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

    public double[] Values(SnowballOption option, BsmModel model, MarketData market, PricingContext context, double[] assetPrices)
    {
        if (option.BarrierTouchStatus == BarrierTouchStatus.UpTouch)
        {
            return new double[assetPrices.Length];
        }

        int count = assetPrices.Length;
        Guard.IsGreaterThanOrEqualTo(count, 3);

        CalculateValue(option, model, market, context);

        double[] values = new double[count];
        ReadOnlySpan<double> priceSpan = PriceVector;
        ReadOnlySpan<double> valueSpan = ValueMatrixSpan.GetRowSpan(0);

        for (int i = 0; i < count; i++)
        {
            values[i] = LinearInterpolation.InterpolateSorted(assetPrices[i], priceSpan, valueSpan);
        }

        return values;
    }

    public double[] Deltas(SnowballOption option, BsmModel model, MarketData market, PricingContext context, double[] assetPrices)
    {
        double[] values = Values(option, model, market, context, assetPrices);
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

    public double[] Gammas(SnowballOption option, BsmModel model, MarketData market, PricingContext context, double[] assetPrices)
    {
        double[] values = Values(option, model, market, context, assetPrices);
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

    protected override double CalculateValue(SnowballOption option, BsmModel model, MarketData market, PricingContext context)
    {
        if (option.BarrierTouchStatus == BarrierTouchStatus.UpTouch)
        {
            return 0.0;
        }

        InitializeParameters(option, context);

        if (option.BarrierTouchStatus == BarrierTouchStatus.DownTouch)
        {
            _isSolvingKnockedIn = true;
            return base.CalculateValue(option, model, market, context);
        }

        // First pass: compute the fully knocked-in surface so it can be referenced when
        // the second pass detects an endogenous knock-in event.
        _isSolvingKnockedIn = true;
        base.CalculateValue(option, model, market, context);

        ValueMatrixSpan.CopyTo(_knockedInValues);

        // Second pass: solve the non-knocked-in scenario, substituting the stored knocked-in surface
        // wherever the price process breaches the knock-in barrier.
        _isSolvingKnockedIn = false;
        return base.CalculateValue(option, model, market, context);
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

    protected override void SetBoundaryConditions(SnowballOption option, BsmModel model)
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
                double coupon = _observationCoupons![nextObsIdx] * _observationAccruedTimes![nextObsIdx];
                ValueMatrixSpan[i, PriceStepCount] = coupon * Math.Exp(-r * (obsTime - t));
            }
            else
            {
                ValueMatrixSpan[i, PriceStepCount] = _maturityPayoff * df;
            }
        }
    }

    protected override void ApplyStepConditions(int i, SnowballOption option, BsmModel model)
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

        if (!_isSolvingKnockedIn)
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
        MaxPrice = Math.Max(option.InitialPrice, maxBarrier) * 4.0;

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
