using CommunityToolkit.Diagnostics;
using DerivaSharp.Instruments;
using MathNet.Numerics;
using MathNet.Numerics.RootFinding;

namespace DerivaSharp.PricingEngines;

public static class SnowballEngineExtensions
{
    extension(PricingEngine<SnowballOption> engine)
    {
        public double ImpliedCouponRate(
            SnowballOption option,
            PricingContext context,
            double optionPrice,
            bool alignMaturityCouponRate,
            double lowerBound = 0.0,
            double upperBound = 1.0,
            double accuracy = 1e-5)
        {
            Guard.IsNotNull(engine);
            Guard.IsNotNull(option);
            Guard.IsLessThan(lowerBound, upperBound);

            double[] baseRates = option.KnockOutCouponRates;
            Guard.IsGreaterThan(baseRates.Length, 0);

            double[] offsets = new double[baseRates.Length];
            double baseFirstRate = baseRates[0];
            for (int i = 0; i < baseRates.Length; i++)
            {
                offsets[i] = baseRates[i] - baseFirstRate;
            }

            try
            {
                return Brent.FindRoot(ObjectiveFunction, lowerBound, upperBound, accuracy);
            }
            catch (NonConvergenceException)
            {
                return double.NaN;
            }

            double ObjectiveFunction(double koRate)
            {
                double[] adjustedRates = new double[offsets.Length];
                for (int i = 0; i < adjustedRates.Length; i++)
                {
                    adjustedRates[i] = koRate + offsets[i];
                }

                SnowballOption candidate = alignMaturityCouponRate
                    ? option with { KnockOutCouponRates = adjustedRates, MaturityCouponRate = adjustedRates[^1] }
                    : option with { KnockOutCouponRates = adjustedRates };

                return engine.Value(candidate, context) - optionPrice;
            }
        }
    }
}
