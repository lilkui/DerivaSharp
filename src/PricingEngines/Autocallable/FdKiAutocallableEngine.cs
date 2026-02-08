using CommunityToolkit.Diagnostics;
using CommunityToolkit.HighPerformance;
using DerivaSharp.Instruments;
using DerivaSharp.Models;
using DerivaSharp.Numerics;

namespace DerivaSharp.PricingEngines;

public abstract class FdKiAutocallableEngine<TOption>(FiniteDifferenceScheme scheme, int priceStepCount, int timeStepCount)
    : BsmFiniteDifferenceEngine<TOption>(scheme, priceStepCount, timeStepCount)
    where TOption : KiAutocallableNote
{
    protected override bool UseTradingDayGrid => true;

    public override double[] Values(TOption option, PricingContext<BsmModelParameters> context, double[] assetPrices)
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
