using CommunityToolkit.Diagnostics;
using CommunityToolkit.HighPerformance;
using DerivaSharp.Instruments;
using DerivaSharp.Models;
using DerivaSharp.Numerics;
using DerivaSharp.Time;
using MathNet.Numerics;

namespace DerivaSharp.PricingEngines;

public abstract class BsmFiniteDifferenceEngine<TOption> : BsmPricingEngine<TOption>
    where TOption : Option
{
    private readonly int _targetTimeStepCount;
    private readonly double _theta;
    private readonly double[] _lower1;
    private readonly double[] _lower2;
    private readonly double[] _main1;
    private readonly double[] _main2;
    private readonly double[] _upper1;
    private readonly double[] _upper2;
    private readonly double[] _rhs;
    private readonly double[] _result;
    private readonly TridiagonalMatrix _m1;
    private readonly TridiagonalMatrix _m2;
    private double[] _valueMatrixBuffer;

    protected BsmFiniteDifferenceEngine(FiniteDifferenceScheme scheme, int priceStepCount, int timeStepCount)
    {
        Guard.IsGreaterThanOrEqualTo(priceStepCount, 2);
        Guard.IsGreaterThanOrEqualTo(timeStepCount, 2);

        Scheme = scheme;
        _theta = scheme switch
        {
            FiniteDifferenceScheme.ExplicitEuler => 0.0,
            FiniteDifferenceScheme.ImplicitEuler => 1.0,
            FiniteDifferenceScheme.CrankNicolson => 0.5,
            _ => ThrowHelper.ThrowArgumentException<double>(ExceptionMessages.InvalidFiniteDifferenceScheme),
        };
        PriceStepCount = priceStepCount;
        TimeStepCount = timeStepCount;
        _targetTimeStepCount = timeStepCount;
        _valueMatrixBuffer = new double[(priceStepCount + 1) * (timeStepCount + 1)];

        int innerSize = priceStepCount - 1;
        _lower1 = new double[innerSize];
        _main1 = new double[innerSize];
        _upper1 = new double[innerSize];
        _lower2 = new double[innerSize];
        _main2 = new double[innerSize];
        _upper2 = new double[innerSize];
        _rhs = new double[innerSize];
        _result = new double[innerSize];
        _m1 = new TridiagonalMatrix(_lower1, _main1, _upper1);
        _m2 = new TridiagonalMatrix(_lower2, _main2, _upper2);
    }

    protected int PriceStepCount { get; }

    protected int TimeStepCount { get; private set; }

    protected double MinPrice { get; set; }

    protected double MaxPrice { get; set; }

    protected double[] PriceVector { get; set; } = [];

    protected double[] TimeVector { get; set; } = [];

    protected FiniteDifferenceScheme Scheme { get; }

    protected Span2D<double> ValueMatrixSpan => new(_valueMatrixBuffer, TimeStepCount + 1, PriceStepCount + 1);

    protected virtual bool UseTradingDayGrid => false;

    protected override double CalculateValue(TOption option, BsmModelParameters parameters, double assetPrice, DateOnly valuationDate)
    {
        SolvePde(option, parameters, valuationDate);
        return LinearInterpolation.InterpolateSorted(assetPrice, PriceVector, ValueMatrixSpan.GetRowSpan(0));
    }

    protected virtual void InitializeGrid(TOption option, BsmModelParameters parameters, DateOnly valuationDate)
    {
        Guard.IsGreaterThanOrEqualTo(MinPrice, 0.0);
        Guard.IsGreaterThan(MaxPrice, MinPrice);

        double tau = GetYearsToExpiration(option, valuationDate);
        PriceVector = Generate.LinearSpaced(PriceStepCount + 1, MinPrice, MaxPrice);

        double maxDt = tau / _targetTimeStepCount;

        if (Scheme == FiniteDifferenceScheme.ExplicitEuler)
        {
            double ds = PriceVector[1] - PriceVector[0];
            double maxDiffusionSquare = parameters.Volatility * parameters.Volatility * MaxPrice * MaxPrice / ds / ds;
            if (maxDt * (maxDiffusionSquare + parameters.RiskFreeRate) > 1.0)
            {
                ThrowHelper.ThrowArgumentException(ExceptionMessages.ExplicitSchemeUnstable);
            }
        }

        TimeVector = BuildTimeGrid(option, valuationDate, tau, maxDt);
        TimeStepCount = TimeVector.Length - 1;
        EnsureValueMatrixBuffer();
    }

    protected abstract void SetTerminalCondition(TOption option);

    protected abstract void SetBoundaryConditions(TOption option, BsmModelParameters parameters);

    protected abstract void ApplyStepConditions(int i, TOption option, BsmModelParameters parameters);

    protected void MapObservationSteps(ReadOnlySpan<double> observationTimes, Span<int> stepToObservationIndex)
    {
        Guard.IsEqualTo(stepToObservationIndex.Length, TimeStepCount + 1);
        stepToObservationIndex.Fill(-1);

        foreach ((int step, int observationIndex) in new ObservationStepEnumerable(this, observationTimes))
        {
            if (stepToObservationIndex[step] != -1)
            {
                ThrowHelper.ThrowArgumentException(ExceptionMessages.MultipleObservationsAtSameStep);
            }

            stepToObservationIndex[step] = observationIndex;
        }
    }

    protected void MapObservationFlags(ReadOnlySpan<double> observationTimes, Span<bool> stepFlags)
    {
        Guard.IsEqualTo(stepFlags.Length, TimeStepCount + 1);
        stepFlags.Clear();

        foreach ((int step, int _) in new ObservationStepEnumerable(this, observationTimes))
        {
            stepFlags[step] = true;
        }
    }

    protected virtual double[] BuildTimeGrid(TOption option, DateOnly valuationDate, double tMax, double maxDt)
    {
        if (tMax <= 0.0)
        {
            return [0.0];
        }

        return UseTradingDayGrid
            ? BuildTradingDayTimeGrid(valuationDate, option.ExpirationDate, tMax, maxDt)
            : BuildUniformTimeGrid(tMax, _targetTimeStepCount);
    }

    private static double[] BuildUniformTimeGrid(double tMax, int stepCount)
    {
        return Generate.LinearSpaced(stepCount + 1, 0.0, tMax);
    }

    private static double[] BuildTradingDayTimeGrid(DateOnly valuationDate, DateOnly expirationDate, double tMax, double maxDt)
    {
        DateOnly[] tradingDays = DateUtils.GetTradingDays(valuationDate, expirationDate).ToArray();
        double[] tradingTimes = new double[tradingDays.Length];

        int t0 = valuationDate.DayNumber;
        for (int i = 0; i < tradingDays.Length; i++)
        {
            tradingTimes[i] = (tradingDays[i].DayNumber - t0) / 365.0;
        }

        return BuildTimeGridFromKeyTimes(tMax, tradingTimes, maxDt);
    }

    private static double[] BuildTimeGridFromKeyTimes(double tMax, ReadOnlySpan<double> keyTimes, double maxDt)
    {
        List<double> keyTimesList = new(keyTimes.Length + 2)
        {
            0.0,
            tMax,
        };
        double tol = GetTimeTolerance(tMax);

        foreach (double t in keyTimes)
        {
            if (t < -tol)
            {
                continue;
            }

            if (t > tMax + tol)
            {
                ThrowHelper.ThrowArgumentException(ExceptionMessages.ObservationTimeNotOnGrid);
            }

            keyTimesList.Add(Math.Clamp(t, 0.0, tMax));
        }

        keyTimesList.Sort();
        List<double> grid = new(keyTimesList.Count + Math.Max(0, keyTimesList.Count - 1));
        grid.Add(keyTimesList[0]);
        double prev = keyTimesList[0];

        for (int i = 1; i < keyTimesList.Count; i++)
        {
            double t = keyTimesList[i];
            if (t - prev <= tol)
            {
                continue;
            }

            double start = prev;
            double end = t;
            double length = end - start;
            int steps = Math.Max(1, (int)Math.Ceiling(length / maxDt));
            double dt = length / steps;

            for (int k = 1; k < steps; k++)
            {
                grid.Add(start + k * dt);
            }

            grid.Add(end);
            prev = end;
        }

        return grid.ToArray();
    }

    private static double GetTimeTolerance(double tMax)
    {
        return Math.Max(1e-12, tMax * 1e-12);
    }

    private void EnsureValueMatrixBuffer()
    {
        int required = (PriceStepCount + 1) * (TimeStepCount + 1);
        if (_valueMatrixBuffer.Length != required)
        {
            _valueMatrixBuffer = new double[required];
        }
    }

    private int GetTimeIndex(double time)
    {
        if (TimeVector.Length == 0)
        {
            ThrowHelper.ThrowArgumentException(ExceptionMessages.ObservationTimeNotOnGrid);
        }

        double tMax = TimeVector[^1];
        double tol = GetTimeTolerance(tMax);

        if (time < -tol)
        {
            return -1;
        }

        if (time > tMax + tol)
        {
            ThrowHelper.ThrowArgumentException(ExceptionMessages.ObservationTimeNotOnGrid);
        }

        time = Math.Clamp(time, 0.0, tMax);

        int candidate = FindNearestTimeIndex(time);

        if (Math.Abs(TimeVector[candidate] - time) > tol)
        {
            ThrowHelper.ThrowArgumentException(ExceptionMessages.ObservationTimeNotOnGrid);
        }

        return candidate;
    }

    private int FindNearestTimeIndex(double time)
    {
        int candidate = TimeVector.BinarySearch(time);

        if (candidate >= 0)
        {
            return candidate;
        }

        int insert = ~candidate;
        if (insert <= 0)
        {
            return 0;
        }

        if (insert >= TimeVector.Length)
        {
            return TimeVector.Length - 1;
        }

        double left = TimeVector[insert - 1];
        double right = TimeVector[insert];
        return Math.Abs(time - left) <= Math.Abs(time - right) ? insert - 1 : insert;
    }

    private void SolveTimeStep(int i, double dt, double ds, BsmModelParameters parameters, Span<double> rhs, Span<double> result, bool updateCoefficients)
    {
        if (updateCoefficients)
        {
            UpdateTridiagonalCoefficients(dt, ds, parameters);
        }

        int length = PriceStepCount - 1;
        ReadOnlySpan<double> prevStepValues = ValueMatrixSpan.GetRowSpan(i + 1).Slice(1, length);

        _m2.Multiply(prevStepValues, rhs);

        rhs[0] = rhs[0] - _m1.Lower[0] * ValueMatrixSpan[i, 0] + _m2.Lower[0] * ValueMatrixSpan[i + 1, 0];
        rhs[^1] = rhs[^1] - _m1.Upper[^1] * ValueMatrixSpan[i, ^1] + _m2.Upper[^1] * ValueMatrixSpan[i + 1, ^1];

        if (Scheme == FiniteDifferenceScheme.ExplicitEuler)
        {
            rhs.CopyTo(result);
        }
        else
        {
            _m1.Solve(rhs, result);
        }

        result.CopyTo(ValueMatrixSpan.GetRowSpan(i).Slice(1, length));
    }

    private void UpdateTridiagonalCoefficients(double dt, double ds, BsmModelParameters parameters)
    {
        double r = parameters.RiskFreeRate;
        double q = parameters.DividendYield;
        double v = parameters.Volatility;
        double oneMinusTheta = 1.0 - _theta;

        for (int j = 1; j < PriceStepCount; j++)
        {
            double s = PriceVector[j];
            double drift = (r - q) * s / ds;
            double diffusionSquare = v * v * s * s / ds / ds;

            double a = 0.5 * dt * (diffusionSquare - drift);
            double b = dt * (diffusionSquare + r);
            double c = 0.5 * dt * (diffusionSquare + drift);

            _lower1[j - 1] = -_theta * a;
            _main1[j - 1] = 1.0 + _theta * b;
            _upper1[j - 1] = -_theta * c;

            _lower2[j - 1] = oneMinusTheta * a;
            _main2[j - 1] = 1.0 - oneMinusTheta * b;
            _upper2[j - 1] = oneMinusTheta * c;
        }
    }

    private void SolvePde(TOption option, BsmModelParameters parameters, DateOnly valuationDate)
    {
        InitializeGrid(option, parameters, valuationDate);
        SetTerminalCondition(option);
        SetBoundaryConditions(option, parameters);
        ApplyStepConditions(TimeStepCount, option, parameters);

        double ds = PriceVector[1] - PriceVector[0];
        if (!UseTradingDayGrid)
        {
            double dt = TimeVector[1] - TimeVector[0];
            UpdateTridiagonalCoefficients(dt, ds, parameters);
        }

        for (int i = TimeStepCount - 1; i >= 0; i--)
        {
            double dt = TimeVector[i + 1] - TimeVector[i];
            SolveTimeStep(i, dt, ds, parameters, _rhs, _result, UseTradingDayGrid);
            ApplyStepConditions(i, option, parameters);
        }
    }

    private readonly ref struct ObservationStepEnumerable(BsmFiniteDifferenceEngine<TOption> engine, ReadOnlySpan<double> observationTimes)
    {
        private readonly ReadOnlySpan<double> _observationTimes = observationTimes;

        public ObservationStepEnumerator GetEnumerator()
        {
            return new ObservationStepEnumerator(engine, _observationTimes);
        }
    }

    private ref struct ObservationStepEnumerator(BsmFiniteDifferenceEngine<TOption> engine, ReadOnlySpan<double> observationTimes)
    {
        private readonly ReadOnlySpan<double> _observationTimes = observationTimes;
        private readonly int _count = observationTimes.Length;
        private int _index = -1;

        public (int Step, int ObservationIndex) Current { get; private set; } = default;

        public bool MoveNext()
        {
            while (++_index < _count)
            {
                double tObs = _observationTimes[_index];
                int step = engine.GetTimeIndex(tObs);
                if (step < 0)
                {
                    continue;
                }

                Current = (step, _index);
                return true;
            }

            return false;
        }
    }
}
