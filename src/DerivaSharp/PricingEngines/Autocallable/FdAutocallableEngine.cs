using CommunityToolkit.Diagnostics;
using CommunityToolkit.HighPerformance;
using DerivaSharp.Instruments;
using DerivaSharp.Models;
using DerivaSharp.Numerics;

namespace DerivaSharp.PricingEngines;

public abstract class FdAutocallableEngine<TOption>(FiniteDifferenceScheme scheme, int priceStepCount, int timeStepCount)
    : FiniteDifference1DPricingEngine<TOption>(scheme, priceStepCount, timeStepCount)
    where TOption : AutocallableNote
{
    public double[] Values(TOption option, PricingContext<BsmModelParameters> context, double[] assetPrices)
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

    public double[] Deltas(TOption option, PricingContext<BsmModelParameters> context, double[] assetPrices)
    {
        double[] values = Values(option, context, assetPrices);
        return FiniteDifferenceGreeks.ComputeDeltas(assetPrices, values);
    }

    public double[] Gammas(TOption option, PricingContext<BsmModelParameters> context, double[] assetPrices)
    {
        double[] values = Values(option, context, assetPrices);
        return FiniteDifferenceGreeks.ComputeGammas(assetPrices, values);
    }

    protected void MapObservationSteps(ReadOnlySpan<double> observationTimes, Span<int> stepToObservationIndex, double tMax)
    {
        stepToObservationIndex.Fill(-1);

        double dt = tMax / TimeStepCount;
        for (int k = 0; k < observationTimes.Length; k++)
        {
            double tObs = observationTimes[k];
            int step = (int)Math.Round(tObs / dt);
            if (step >= 0 && step <= TimeStepCount && Math.Abs(step * dt - tObs) < dt / 2.0)
            {
                stepToObservationIndex[step] = k;
            }
        }
    }

    protected void ApplyKnockInSubstitution(int i, double knockInPrice, bool apply, double[] knockedInValues)
    {
        if (!apply)
        {
            return;
        }

        ReadOnlySpan2D<double> knockInMatrix = new(knockedInValues, TimeStepCount + 1, PriceStepCount + 1);
        for (int j = 0; j <= PriceStepCount; j++)
        {
            if (PriceVector[j] < knockInPrice)
            {
                ValueMatrixSpan[i, j] = knockInMatrix[i, j];
            }
        }
    }
}
