using CommunityToolkit.Diagnostics;
using DerivaSharp.Instruments;
using DerivaSharp.Models;
using MathNet.Numerics;
using MathNet.Numerics.RootFinding;

namespace DerivaSharp.PricingEngines;

/// <summary>
///     Extension methods for autocallable option pricing engines.
/// </summary>
public static class AutocallableEngineExtensions
{
    extension(PricingEngine<SnowballOption, BsmModelParameters> engine)
    {
        /// <summary>
        ///     Calculates the implied knock-out coupon rate for a snowball option given a target price.
        /// </summary>
        /// <param name="option">The snowball option.</param>
        /// <param name="context">The pricing context.</param>
        /// <param name="optionPrice">The target option price.</param>
        /// <param name="alignMaturityCouponRate">Whether to align the maturity coupon rate with the final knock-out rate.</param>
        /// <param name="lowerBound">Lower bound for the root-finding search.</param>
        /// <param name="upperBound">Upper bound for the root-finding search.</param>
        /// <param name="accuracy">Desired accuracy for the root-finding algorithm.</param>
        /// <returns>The implied coupon rate, or <see cref="double.NaN" /> if the algorithm does not converge.</returns>
        public double ImpliedCouponRate(
            SnowballOption option,
            PricingContext<BsmModelParameters> context,
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
                    ? option with
                    {
                        KnockOutCouponRates = adjustedRates,
                        MaturityCouponRate = adjustedRates[^1],
                    }
                    : option with { KnockOutCouponRates = adjustedRates };

                return engine.Value(candidate, context) - optionPrice;
            }
        }
    }

    extension(PricingEngine<PhoenixOption, BsmModelParameters> engine)
    {
        /// <summary>
        ///     Calculates the implied coupon rate for a phoenix option given a target price.
        /// </summary>
        /// <param name="option">The phoenix option.</param>
        /// <param name="context">The pricing context.</param>
        /// <param name="optionPrice">The target option price.</param>
        /// <param name="lowerBound">Lower bound for the root-finding search.</param>
        /// <param name="upperBound">Upper bound for the root-finding search.</param>
        /// <param name="accuracy">Desired accuracy for the root-finding algorithm.</param>
        /// <returns>The implied coupon rate, or <see cref="double.NaN" /> if the algorithm does not converge.</returns>
        public double ImpliedCouponRate(
            PhoenixOption option,
            PricingContext<BsmModelParameters> context,
            double optionPrice,
            double lowerBound = 0.0,
            double upperBound = 1.0,
            double accuracy = 1e-5)
        {
            Guard.IsNotNull(engine);
            Guard.IsNotNull(option);
            Guard.IsLessThan(lowerBound, upperBound);

            try
            {
                return Brent.FindRoot(ObjectiveFunction, lowerBound, upperBound, accuracy);
            }
            catch (NonConvergenceException)
            {
                return double.NaN;
            }

            double ObjectiveFunction(double rate)
            {
                PhoenixOption candidate = option with { CouponRate = rate };

                return engine.Value(candidate, context) - optionPrice;
            }
        }
    }
}
