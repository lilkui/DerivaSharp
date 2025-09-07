using System.Diagnostics;
using CommunityToolkit.Diagnostics;
using CommunityToolkit.HighPerformance;
using DerivaSharp.Instruments;
using DerivaSharp.Numerics;
using MathNet.Numerics;

namespace DerivaSharp.PricingEngines;

public abstract class FiniteDifferencePricingEngine<TOption> : PricingEngine<TOption>
    where TOption : Option
{
    private readonly double[] _lower1;
    private readonly double[] _lower2;
    private readonly double[] _main1;
    private readonly double[] _main2;
    private readonly double[] _result;
    private readonly double[] _rhs;
    private readonly double[] _upper1;
    private readonly double[] _upper2;
    private readonly double[] _valueMatrixBuffer;

    private TridiagonalMatrix? _m1;
    private TridiagonalMatrix? _m2;

    protected FiniteDifferencePricingEngine(FiniteDifferenceScheme scheme, int priceStepCount, int timeStepCount)
    {
        Guard.IsGreaterThan(priceStepCount, 0);
        Guard.IsGreaterThan(timeStepCount, 0);

        Scheme = scheme;
        PriceStepCount = priceStepCount;
        TimeStepCount = timeStepCount;

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
    }

    protected int PriceStepCount { get; }

    protected int TimeStepCount { get; }

    protected double MinPrice { get; set; }

    protected double MaxPrice { get; set; }

    protected double[] PriceVector { get; set; } = null!;

    protected double[] TimeVector { get; set; } = null!;

    protected FiniteDifferenceScheme Scheme { get; }

    protected Span2D<double> ValueMatrixSpan => new(_valueMatrixBuffer, TimeStepCount + 1, PriceStepCount + 1);

    protected override double CalculateValue(TOption option, PricingContext context)
    {
        SolvePde(option, context);
        return LinearInterpolation.InterpolateSorted(context.AssetPrice, PriceVector, ValueMatrixSpan.GetRowSpan(0));
    }

    protected virtual void InitializeCoefficients(TOption option, PricingContext context)
    {
        Guard.IsGreaterThanOrEqualTo(MinPrice, 0.0);
        Guard.IsGreaterThan(MaxPrice, MinPrice);

        double tau = GetYearsToExpiration(option, context);
        PriceVector = Generate.LinearSpaced(PriceStepCount + 1, MinPrice, MaxPrice);
        TimeVector = Generate.LinearSpaced(TimeStepCount + 1, 0, tau);

        double ds = PriceVector[1] - PriceVector[0];
        double dt = TimeVector[1] - TimeVector[0];
        double r = context.RiskFreeRate;
        double q = context.DividendYield;
        double v = context.Volatility;

        double theta = Scheme switch
        {
            FiniteDifferenceScheme.ExplicitEuler => 0.0,
            FiniteDifferenceScheme.ImplicitEuler => 1.0,
            FiniteDifferenceScheme.CrankNicolson => 0.5,
            _ => ThrowHelper.ThrowArgumentException<double>(ExceptionMessages.InvalidFiniteDifferenceScheme),
        };

        if (Scheme == FiniteDifferenceScheme.ExplicitEuler)
        {
            double maxDiffusionSquare = v * v * MaxPrice * MaxPrice / ds / ds;
            if (dt * (maxDiffusionSquare + r) > 1.0)
            {
                ThrowHelper.ThrowArgumentException(ExceptionMessages.ExplicitSchemeUnstable);
            }
        }

        for (int j = 1; j < PriceStepCount; j++)
        {
            double s = PriceVector[j];
            double drift = (r - q) * s / ds;
            double diffusionSquare = v * v * s * s / ds / ds;

            double a = 0.5 * dt * (diffusionSquare - drift);
            double b = dt * (diffusionSquare + r);
            double c = 0.5 * dt * (diffusionSquare + drift);

            _lower1[j - 1] = -theta * a;
            _main1[j - 1] = 1.0 + theta * b;
            _upper1[j - 1] = -theta * c;

            _lower2[j - 1] = (1 - theta) * a;
            _main2[j - 1] = 1.0 - (1 - theta) * b;
            _upper2[j - 1] = (1 - theta) * c;
        }

        _m1 = new TridiagonalMatrix(_lower1, _main1, _upper1);
        _m2 = new TridiagonalMatrix(_lower2, _main2, _upper2);
    }

    protected abstract void SetTerminalCondition(TOption option);

    protected abstract void SetBoundaryConditions(TOption option, PricingContext context);

    protected abstract void ApplyStepConditions(int i, TOption option, PricingContext context);

    private void SolveSingleStep(int i, Span<double> rhs, Span<double> result)
    {
        Debug.Assert(_m1 is not null);
        Debug.Assert(_m2 is not null);

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

    private void SolvePde(TOption option, PricingContext context)
    {
        InitializeCoefficients(option, context);
        SetTerminalCondition(option);
        SetBoundaryConditions(option, context);

        for (int i = TimeStepCount - 1; i >= 0; i--)
        {
            ApplyStepConditions(i + 1, option, context);
            SolveSingleStep(i, _rhs, _result);
        }

        ApplyStepConditions(0, option, context);
    }
}
