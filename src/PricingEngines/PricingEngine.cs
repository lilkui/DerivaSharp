using CommunityToolkit.Diagnostics;
using DerivaSharp.Instruments;
using DerivaSharp.Models;

namespace DerivaSharp.PricingEngines;

/// <summary>
///     Base class for option pricing engines.
/// </summary>
/// <typeparam name="TOption">The type of option to price.</typeparam>
/// <typeparam name="TModel">The type of model parameters.</typeparam>
public abstract class PricingEngine<TOption, TModel>
    where TOption : Option
    where TModel : IModelParameters
{
    /// <summary>
    ///     Gets or initializes the shift parameters used for numerical differentiation.
    /// </summary>
    protected NumericalShiftParameters ShiftParameters { get; init; } = NumericalShiftParameters.Default;

    /// <summary>
    ///     Computes the option value.
    /// </summary>
    /// <param name="option">The option to price.</param>
    /// <param name="context">The pricing context.</param>
    /// <returns>The option value.</returns>
    public double Value(TOption option, in PricingContext<TModel> context)
    {
        ValidateArguments(option, context);

        return CalculateValue(option, context);
    }

    /// <summary>
    ///     Computes delta using numerical differentiation.
    /// </summary>
    /// <param name="option">The option to price.</param>
    /// <param name="context">The pricing context.</param>
    /// <returns>The rate of change of value with respect to the asset price.</returns>
    public virtual double Delta(TOption option, in PricingContext<TModel> context)
    {
        ValidateArguments(option, context);

        double s = context.AssetPrice;
        double ds = s * ShiftParameters.AssetPriceShiftFactor;

        double vSpotPlus = CalculateValue(option, context with { AssetPrice = s + ds });
        double vSpotMinus = CalculateValue(option, context with { AssetPrice = s - ds });

        return (vSpotPlus - vSpotMinus) / (2 * ds);
    }

    /// <summary>
    ///     Computes gamma using numerical differentiation.
    /// </summary>
    /// <param name="option">The option to price.</param>
    /// <param name="context">The pricing context.</param>
    /// <returns>The rate of change of delta with respect to the asset price.</returns>
    public virtual double Gamma(TOption option, in PricingContext<TModel> context)
    {
        ValidateArguments(option, context);

        double s = context.AssetPrice;
        double ds = s * ShiftParameters.AssetPriceShiftFactor;

        double v0 = CalculateValue(option, context);
        double vSpotPlus = CalculateValue(option, context with { AssetPrice = s + ds });
        double vSpotMinus = CalculateValue(option, context with { AssetPrice = s - ds });

        return (vSpotPlus - 2 * v0 + vSpotMinus) / (ds * ds);
    }

    /// <summary>
    ///     Computes speed using numerical differentiation.
    /// </summary>
    /// <param name="option">The option to price.</param>
    /// <param name="context">The pricing context.</param>
    /// <returns>The rate of change of gamma with respect to the asset price.</returns>
    public virtual double Speed(TOption option, in PricingContext<TModel> context)
    {
        ValidateArguments(option, context);

        double s = context.AssetPrice;
        double ds = s * ShiftParameters.AssetPriceShiftFactor;

        double v0 = CalculateValue(option, context);
        double vSpotPlus = CalculateValue(option, context with { AssetPrice = s + ds });
        double vSpotPlusPlus = CalculateValue(option, context with { AssetPrice = s + 2 * ds });
        double vSpotMinus = CalculateValue(option, context with { AssetPrice = s - ds });

        return (vSpotPlusPlus - 3 * vSpotPlus + 3 * v0 - vSpotMinus) / (ds * ds * ds);
    }

    /// <summary>
    ///     Computes theta using numerical differentiation.
    /// </summary>
    /// <param name="option">The option to price.</param>
    /// <param name="context">The pricing context.</param>
    /// <returns>The rate of change of value with respect to time (per day).</returns>
    public virtual double Theta(TOption option, in PricingContext<TModel> context)
    {
        ValidateArguments(option, context);

        DateOnly t = context.ValuationDate;

        double v0 = CalculateValue(option, context);

        bool atExpiry = t >= option.ExpirationDate;
        double vNext = atExpiry ? 0 : CalculateValue(option, context with { ValuationDate = t.AddDays(1) });

        return vNext - v0;
    }

    /// <summary>
    ///     Computes charm using numerical differentiation.
    /// </summary>
    /// <param name="option">The option to price.</param>
    /// <param name="context">The pricing context.</param>
    /// <returns>The rate of change of delta with respect to time (per day).</returns>
    public virtual double Charm(TOption option, in PricingContext<TModel> context)
    {
        ValidateArguments(option, context);

        double s = context.AssetPrice;
        DateOnly t = context.ValuationDate;
        double ds = s * ShiftParameters.AssetPriceShiftFactor;

        double vSpotPlus = CalculateValue(option, context with { AssetPrice = s + ds });
        double vSpotMinus = CalculateValue(option, context with { AssetPrice = s - ds });

        bool atExpiry = t >= option.ExpirationDate;
        double vSpotPlusNext = atExpiry
            ? 0
            : CalculateValue(option, context with
            {
                AssetPrice = s + ds,
                ValuationDate = t.AddDays(1),
            });
        double vSpotMinusNext = atExpiry
            ? 0
            : CalculateValue(option, context with
            {
                AssetPrice = s - ds,
                ValuationDate = t.AddDays(1),
            });

        return (vSpotPlusNext - vSpotPlus - vSpotMinusNext + vSpotMinus) / (2 * ds);
    }

    /// <summary>
    ///     Computes color using numerical differentiation.
    /// </summary>
    /// <param name="option">The option to price.</param>
    /// <param name="context">The pricing context.</param>
    /// <returns>The rate of change of gamma with respect to time (per day).</returns>
    public virtual double Color(TOption option, in PricingContext<TModel> context)
    {
        ValidateArguments(option, context);

        double s = context.AssetPrice;
        DateOnly t = context.ValuationDate;
        double ds = s * ShiftParameters.AssetPriceShiftFactor;

        double v0 = CalculateValue(option, context);
        double vSpotPlus = CalculateValue(option, context with { AssetPrice = s + ds });
        double vSpotMinus = CalculateValue(option, context with { AssetPrice = s - ds });

        bool atExpiry = t >= option.ExpirationDate;
        double vNext = atExpiry ? 0 : CalculateValue(option, context with { ValuationDate = t.AddDays(1) });
        double vSpotPlusNext = atExpiry
            ? 0
            : CalculateValue(option, context with
            {
                AssetPrice = s + ds,
                ValuationDate = t.AddDays(1),
            });
        double vSpotMinusNext = atExpiry
            ? 0
            : CalculateValue(option, context with
            {
                AssetPrice = s - ds,
                ValuationDate = t.AddDays(1),
            });

        return (vSpotPlusNext - 2 * vNext + vSpotMinusNext - vSpotPlus + 2 * v0 - vSpotMinus) / (ds * ds);
    }

    /// <summary>
    ///     Computes option values across a range of asset prices.
    /// </summary>
    /// <param name="option">The option to price.</param>
    /// <param name="context">The pricing context.</param>
    /// <param name="assetPrices">The array of asset prices.</param>
    /// <returns>An array of option values corresponding to each asset price.</returns>
    public virtual double[] Values(TOption option, in PricingContext<TModel> context, double[] assetPrices)
    {
        Guard.IsGreaterThanOrEqualTo(assetPrices.Length, 3);

        double[] values = new double[assetPrices.Length];
        for (int i = 0; i < assetPrices.Length; i++)
        {
            values[i] = Value(option, context with { AssetPrice = assetPrices[i] });
        }

        return values;
    }

    /// <summary>
    ///     Computes deltas across a range of asset prices using finite differences.
    /// </summary>
    /// <param name="option">The option to price.</param>
    /// <param name="context">The pricing context.</param>
    /// <param name="assetPrices">The array of asset prices.</param>
    /// <returns>An array of deltas corresponding to each asset price.</returns>
    public virtual double[] Deltas(TOption option, in PricingContext<TModel> context, double[] assetPrices)
    {
        double[] values = Values(option, context, assetPrices);
        int count = assetPrices.Length;
        double ds = assetPrices[1] - assetPrices[0];

        double[] deltas = new double[count];
        deltas[0] = (values[1] - values[0]) / ds;
        deltas[^1] = (values[^1] - values[^2]) / ds;

        for (int i = 1; i < count - 1; i++)
        {
            deltas[i] = (values[i + 1] - values[i - 1]) / (2 * ds);
        }

        return deltas;
    }

    /// <summary>
    ///     Computes gammas across a range of asset prices using finite differences.
    /// </summary>
    /// <param name="option">The option to price.</param>
    /// <param name="context">The pricing context.</param>
    /// <param name="assetPrices">The array of asset prices.</param>
    /// <returns>An array of gammas corresponding to each asset price.</returns>
    public virtual double[] Gammas(TOption option, in PricingContext<TModel> context, double[] assetPrices)
    {
        double[] values = Values(option, context, assetPrices);
        int count = assetPrices.Length;
        double ds = assetPrices[1] - assetPrices[0];
        double dsSquared = ds * ds;

        double[] gammas = new double[count];
        gammas[0] = (values[2] - 2 * values[1] + values[0]) / dsSquared;
        gammas[^1] = (values[^1] - 2 * values[^2] + values[^3]) / dsSquared;

        for (int i = 1; i < count - 1; i++)
        {
            gammas[i] = (values[i + 1] - 2 * values[i] + values[i - 1]) / dsSquared;
        }

        return gammas;
    }

    /// <summary>
    ///     Computes the option value for the given parameters.
    /// </summary>
    /// <param name="option">The option to price.</param>
    /// <param name="context">The pricing context.</param>
    /// <returns>The option value.</returns>
    protected abstract double CalculateValue(TOption option, in PricingContext<TModel> context);

    /// <summary>
    ///     Validates the option and pricing context.
    /// </summary>
    /// <param name="option">The option to validate.</param>
    /// <param name="context">The pricing context to validate.</param>
    protected virtual void ValidateArguments(TOption option, in PricingContext<TModel> context)
    {
        Guard.IsNotNull(option);
        Guard.IsBetweenOrEqualTo(context.ValuationDate, option.EffectiveDate, option.ExpirationDate);
    }
}
