using CommunityToolkit.Diagnostics;
using DerivaSharp.Instruments;
using DerivaSharp.Models;
using DerivaSharp.Numerics;

namespace DerivaSharp.PricingEngines;

public sealed class FdBinarySnowballEngine(FiniteDifferenceScheme scheme, int priceStepCount, int timeStepCount)
    : BsmFiniteDifferenceEngine<BinarySnowballOption>(scheme, priceStepCount, timeStepCount)
{
    private int[] _stepToObservationIndex = [];
    private double[]? _observationTimes;
    private double[]? _observationPrices;
    private double[]? _observationCoupons;
    private double[]? _observationAccruedTimes;
    private double _maturityPayoff;

    protected override bool UseTradingDayGrid => true;

    public override double[] Values(BinarySnowballOption option, PricingContext<BsmModelParameters> context, double[] assetPrices)
    {
        if (option.BarrierTouchStatus == BarrierTouchStatus.UpTouch)
        {
            return new double[assetPrices.Length];
        }

        int count = assetPrices.Length;
        Guard.IsGreaterThanOrEqualTo(count, 3);

        CalculateValue(option, context.ModelParameters, context.AssetPrice, context.ValuationDate);

        double[] values = new double[count];
        ReadOnlySpan<double> priceSpan = PriceVector;
        ReadOnlySpan<double> valueSpan = ValueMatrixSpan.GetRowSpan(0);

        for (int i = 0; i < count; i++)
        {
            values[i] = LinearInterpolation.InterpolateSorted(assetPrices[i], priceSpan, valueSpan);
        }

        return values;
    }

    protected override double CalculateValue(BinarySnowballOption option, BsmModelParameters parameters, double assetPrice, DateOnly valuationDate)
    {
        if (option.BarrierTouchStatus == BarrierTouchStatus.UpTouch)
        {
            return 0.0;
        }

        InitializeParameters(option, valuationDate);

        return base.CalculateValue(option, parameters, assetPrice, valuationDate);
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
        int nObs = _observationTimes!.Length;

        for (int i = 0; i <= TimeStepCount; i++)
        {
            double t = TimeVector[i];
            double df = Math.Exp(-r * (maturity - t));

            ValueMatrixSpan[i, 0] = _maturityPayoff * df;

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

    protected override void ApplyStepConditions(int i, BinarySnowballOption option, BsmModelParameters parameters)
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
    }

    protected override void InitializeCoefficients(BinarySnowballOption option, BsmModelParameters parameters, DateOnly valuationDate)
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

        MapObservationSteps(_observationTimes!, _stepToObservationIndex);
    }

    private void InitializeParameters(BinarySnowballOption option, DateOnly valuationDate)
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

        MinPrice = 0.0;
        double maxBarrier = n > 0 ? option.KnockOutPrices.Max() : option.InitialPrice;
        MaxPrice = Math.Max(option.InitialPrice, maxBarrier) * 4.0;
    }
}
