using CommunityToolkit.Diagnostics;
using DerivaSharp.Instruments;
using DerivaSharp.Models;

namespace DerivaSharp.PricingEngines;

public sealed class BinomialTreeVanillaEngine : BsmPricingEngine<VanillaOption>
{
    private readonly int _stepCount;
    private readonly double[] _prices;
    private readonly double[] _values;

    public BinomialTreeVanillaEngine(int stepCount)
    {
        Guard.IsGreaterThan(stepCount, 0);

        _stepCount = stepCount;
        _prices = new double[stepCount + 1];
        _values = new double[stepCount + 1];
    }

    protected override double CalculateValue(VanillaOption option, BsmModel model, MarketData market, PricingContext context)
    {
        double x = option.StrikePrice;
        int z = (int)option.OptionType;
        double s0 = market.AssetPrice;
        double tau = GetYearsToExpiration(option, context);

        if (tau == 0)
        {
            return Math.Max(z * (s0 - x), 0);
        }

        double vol = model.Volatility;
        double r = model.RiskFreeRate;
        double q = model.DividendYield;
        bool isAmerican = option.Exercise == Exercise.American;

        double dt = tau / _stepCount;
        double u = Math.Exp(vol * Math.Sqrt(dt));
        double d = 1.0 / u;
        double p = (Math.Exp((r - q) * dt) - d) / (u - d);
        double df = Math.Exp(-r * dt);

        Guard.IsBetweenOrEqualTo(p, 0.0, 1.0);

        // Asset prices at maturity
        double f = u / d;
        _prices[0] = s0 * Math.Pow(d, _stepCount);
        for (int i = 1; i <= _stepCount; i++)
        {
            _prices[i] = _prices[i - 1] * f;
        }

        // Option values at maturity
        for (int i = 0; i <= _stepCount; i++)
        {
            _values[i] = Math.Max(z * (_prices[i] - x), 0);
        }

        // Backward induction
        for (int step = _stepCount - 1; step >= 0; step--)
        {
            for (int i = 0; i <= step; i++)
            {
                _prices[i] *= d; // move to previous step
                double continuation = df * (p * _values[i + 1] + (1 - p) * _values[i]);
                if (isAmerican)
                {
                    double exerciseValue = Math.Max(z * (_prices[i] - x), 0);
                    _values[i] = Math.Max(exerciseValue, continuation);
                }
                else
                {
                    _values[i] = continuation;
                }
            }
        }

        return _values[0];
    }
}
