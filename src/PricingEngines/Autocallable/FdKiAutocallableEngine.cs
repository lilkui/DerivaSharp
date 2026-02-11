using CommunityToolkit.Diagnostics;
using CommunityToolkit.HighPerformance;
using DerivaSharp.Instruments;
using DerivaSharp.Models;
using DerivaSharp.Numerics;

namespace DerivaSharp.PricingEngines;

/// <summary>
///     Base class for finite difference pricing engines for knock-in autocallable notes.
/// </summary>
/// <typeparam name="TOption">The type of knock-in autocallable note to price.</typeparam>
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

    /// <summary>
    ///     Applies knock-in substitution by replacing values below the knock-in price with knocked-in values.
    /// </summary>
    /// <param name="i">The time step index.</param>
    /// <param name="knockInPrice">The knock-in barrier price.</param>
    /// <param name="apply">Whether to apply the substitution.</param>
    /// <param name="knockedInValues">The values to use when knocked in.</param>
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
