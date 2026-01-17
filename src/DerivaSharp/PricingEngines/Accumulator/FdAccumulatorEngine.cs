using CommunityToolkit.Diagnostics;
using CommunityToolkit.HighPerformance;
using DerivaSharp.Instruments;
using DerivaSharp.Models;
using DerivaSharp.Numerics;
using DerivaSharp.Time;

namespace DerivaSharp.PricingEngines;

public sealed class FdAccumulatorEngine : BsmPricingEngine<Accumulator>
{
    private readonly FiniteDifferenceScheme _scheme;
    private readonly int _priceStepCount;
    private readonly int _timeStepCount;

    public FdAccumulatorEngine(FiniteDifferenceScheme scheme, int priceStepCount, int timeStepCount)
    {
        Guard.IsGreaterThanOrEqualTo(priceStepCount, 2);
        Guard.IsGreaterThanOrEqualTo(timeStepCount, 2);

        _scheme = scheme;
        _priceStepCount = priceStepCount;
        _timeStepCount = timeStepCount;
    }

    public override double[] Values(Accumulator option, PricingContext<BsmModelParameters> context, double[] assetPrices)
    {
        Guard.IsGreaterThanOrEqualTo(assetPrices.Length, 3);

        if (context.ValuationDate == option.ExpirationDate)
        {
            double[] terminal = new double[assetPrices.Length];
            for (int i = 0; i < assetPrices.Length; i++)
            {
                terminal[i] = option.AccumulatedQuantity * (assetPrices[i] - option.StrikePrice);
            }

            return terminal;
        }

        AccumulatorAEngine aEngine = new(_scheme, _priceStepCount, _timeStepCount);
        aEngine.Solve(option, context.ModelParameters, context.AssetPrice, context.ValuationDate);

        double[] aMatrix = aEngine.GetValueMatrixCopy();
        ReadOnlySpan<double> aRow = aEngine.GetRow0Span();
        ReadOnlySpan<double> priceVector = aEngine.PriceGrid;

        AccumulatorBEngine bEngine = new(_scheme, _priceStepCount, _timeStepCount, aMatrix);
        bEngine.Solve(option, context.ModelParameters, context.AssetPrice, context.ValuationDate);

        ReadOnlySpan<double> bRow = bEngine.GetRow0Span();

        double[] values = new double[assetPrices.Length];
        for (int i = 0; i < assetPrices.Length; i++)
        {
            double aValue = LinearInterpolation.InterpolateSorted(assetPrices[i], priceVector, aRow);
            double bValue = LinearInterpolation.InterpolateSorted(assetPrices[i], priceVector, bRow);
            values[i] = option.AccumulatedQuantity * aValue + bValue;
        }

        return values;
    }

    protected override double CalculateValue(Accumulator option, BsmModelParameters parameters, double assetPrice, DateOnly valuationDate)
    {
        if (valuationDate == option.ExpirationDate)
        {
            return option.AccumulatedQuantity * (assetPrice - option.StrikePrice);
        }

        AccumulatorAEngine aEngine = new(_scheme, _priceStepCount, _timeStepCount);
        double aValue = aEngine.Solve(option, parameters, assetPrice, valuationDate);
        double[] aMatrix = aEngine.GetValueMatrixCopy();

        AccumulatorBEngine bEngine = new(_scheme, _priceStepCount, _timeStepCount, aMatrix);
        double bValue = bEngine.Solve(option, parameters, assetPrice, valuationDate);

        return option.AccumulatedQuantity * aValue + bValue;
    }

    private static void BuildObservationSteps(DateOnly valuationDate, DateOnly expirationDate, Span<int> stepToObservationIndex, int timeStepCount)
    {
        stepToObservationIndex.Fill(-1);

        if (timeStepCount <= 0)
        {
            return;
        }

        DateOnly[] tradingDays = DateUtils.GetTradingDays(valuationDate, expirationDate).ToArray();
        if (tradingDays.Length == 0)
        {
            return;
        }

        double[] observationTimes = new double[tradingDays.Length];
        int t0 = valuationDate.DayNumber;
        for (int i = 0; i < tradingDays.Length; i++)
        {
            observationTimes[i] = (tradingDays[i].DayNumber - t0) / 365.0;
        }

        double tMax = (expirationDate.DayNumber - t0) / 365.0;
        double dt = tMax / timeStepCount;

        for (int k = 0; k < observationTimes.Length; k++)
        {
            double tObs = observationTimes[k];
            int step = (int)Math.Round(tObs / dt);
            if (step >= 0 && step <= timeStepCount && Math.Abs(step * dt - tObs) < dt / 2.0)
            {
                stepToObservationIndex[step] = k;
            }
        }
    }

    private sealed class AccumulatorAEngine(FiniteDifferenceScheme scheme, int priceStepCount, int timeStepCount)
        : FiniteDifference1DPricingEngine<Accumulator>(scheme, priceStepCount, timeStepCount)
    {
        private readonly int[] _stepToObservationIndex = new int[timeStepCount + 1];
        private double _strike;
        private double _knockOut;

        public double[] PriceGrid => PriceVector;

        public double Solve(Accumulator option, BsmModelParameters parameters, double assetPrice, DateOnly valuationDate) =>
            CalculateValue(option, parameters, assetPrice, valuationDate);

        public ReadOnlySpan<double> GetRow0Span() => ValueMatrixSpan.GetRowSpan(0);

        public double[] GetValueMatrixCopy()
        {
            double[] buffer = new double[(TimeStepCount + 1) * (PriceStepCount + 1)];
            ValueMatrixSpan.CopyTo(buffer);
            return buffer;
        }

        protected override void InitializeCoefficients(Accumulator option, BsmModelParameters parameters, DateOnly valuationDate)
        {
            _strike = option.StrikePrice;
            _knockOut = option.KnockOutPrice;

            MinPrice = 0.0;
            double maxRef = Math.Max(option.StrikePrice, option.KnockOutPrice);
            MaxPrice = 4.0 * maxRef;

            base.InitializeCoefficients(option, parameters, valuationDate);

            BuildObservationSteps(valuationDate, option.ExpirationDate, _stepToObservationIndex, TimeStepCount);
        }

        protected override void SetTerminalCondition(Accumulator option)
        {
            for (int j = 0; j <= PriceStepCount; j++)
            {
                ValueMatrixSpan[TimeStepCount, j] = PriceVector[j] - _strike;
            }
        }

        protected override void SetBoundaryConditions(Accumulator option, BsmModelParameters parameters)
        {
            double r = parameters.RiskFreeRate;
            double q = parameters.DividendYield;
            double maxTime = TimeVector[^1];
            double maxPrice = PriceVector[^1];

            for (int i = 0; i < TimeVector.Length - 1; i++)
            {
                double tau = maxTime - TimeVector[i];
                double dfq = Math.Exp(-q * tau);
                double dfr = Math.Exp(-r * tau);

                ValueMatrixSpan[i, 0] = -_strike * dfr;
                ValueMatrixSpan[i, ^1] = maxPrice * dfq - _strike * dfr;
            }
        }

        protected override void ApplyStepConditions(int i, Accumulator option, BsmModelParameters parameters)
        {
            if (_stepToObservationIndex[i] == -1)
            {
                return;
            }

            Span<double> row = ValueMatrixSpan.GetRowSpan(i);
            for (int j = 0; j <= PriceStepCount; j++)
            {
                if (PriceVector[j] >= _knockOut)
                {
                    row[j] = PriceVector[j] - _strike;
                }
            }
        }
    }

    private sealed class AccumulatorBEngine(FiniteDifferenceScheme scheme, int priceStepCount, int timeStepCount, double[] aValues)
        : FiniteDifference1DPricingEngine<Accumulator>(scheme, priceStepCount, timeStepCount)
    {
        private readonly double[] _aValues = aValues;
        private readonly int[] _stepToObservationIndex = new int[timeStepCount + 1];
        private double _strike;
        private double _knockOut;
        private double _dailyQuantity;
        private double _accelerationFactor;

        public double Solve(Accumulator option, BsmModelParameters parameters, double assetPrice, DateOnly valuationDate) =>
            CalculateValue(option, parameters, assetPrice, valuationDate);

        public ReadOnlySpan<double> GetRow0Span() => ValueMatrixSpan.GetRowSpan(0);

        protected override void InitializeCoefficients(Accumulator option, BsmModelParameters parameters, DateOnly valuationDate)
        {
            _strike = option.StrikePrice;
            _knockOut = option.KnockOutPrice;
            _dailyQuantity = option.DailyQuantity;
            _accelerationFactor = option.AccelerationFactor;

            MinPrice = 0.0;
            double maxRef = Math.Max(option.StrikePrice, option.KnockOutPrice);
            MaxPrice = 4.0 * maxRef;

            base.InitializeCoefficients(option, parameters, valuationDate);

            BuildObservationSteps(valuationDate, option.ExpirationDate, _stepToObservationIndex, TimeStepCount);
        }

        protected override void SetTerminalCondition(Accumulator option)
        {
            for (int j = 0; j <= PriceStepCount; j++)
            {
                ValueMatrixSpan[TimeStepCount, j] = 0.0;
            }
        }

        protected override void SetBoundaryConditions(Accumulator option, BsmModelParameters parameters)
        {
            for (int i = 0; i < TimeVector.Length - 1; i++)
            {
                ValueMatrixSpan[i, 0] = 0.0;
                ValueMatrixSpan[i, ^1] = 0.0;
            }
        }

        protected override void ApplyStepConditions(int i, Accumulator option, BsmModelParameters parameters)
        {
            if (_stepToObservationIndex[i] == -1)
            {
                return;
            }

            ReadOnlySpan2D<double> aMatrix = new(_aValues, TimeStepCount + 1, PriceStepCount + 1);
            Span<double> row = ValueMatrixSpan.GetRowSpan(i);

            for (int j = 0; j <= PriceStepCount; j++)
            {
                double s = PriceVector[j];

                if (s >= _knockOut)
                {
                    row[j] = 0.0;
                    continue;
                }

                double dq = s < _strike ? _dailyQuantity * _accelerationFactor : _dailyQuantity;
                row[j] += dq * aMatrix[i, j];
            }
        }
    }
}
