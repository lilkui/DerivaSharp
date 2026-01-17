using CommunityToolkit.Diagnostics;
using DerivaSharp.Instruments;
using DerivaSharp.Models;

namespace DerivaSharp.PricingEngines;

public abstract class PricingEngine<TOption, TModel>
    where TOption : Option
    where TModel : IModelParameters
{
    public double Value(TOption option, PricingContext<TModel> context)
    {
        ValidateArguments(option, context);

        return CalculateValue(option, context.ModelParameters, context.AssetPrice, context.ValuationDate);
    }

    // DValueDSpot
    public virtual double Delta(TOption option, PricingContext<TModel> context)
    {
        ValidateArguments(option, context);

        TModel model = context.ModelParameters;
        double s = context.AssetPrice;
        double ds = s * NumericalApproximationParameters.AssetPriceShiftFactor;

        double vSpotPlus = CalculateValue(option, model, s + ds, context.ValuationDate);
        double vSpotMinus = CalculateValue(option, model, s - ds, context.ValuationDate);

        return (vSpotPlus - vSpotMinus) / (2 * ds);
    }

    // DDeltaDSpot
    public virtual double Gamma(TOption option, PricingContext<TModel> context)
    {
        ValidateArguments(option, context);

        TModel model = context.ModelParameters;
        double s = context.AssetPrice;
        double ds = s * NumericalApproximationParameters.AssetPriceShiftFactor;

        double v0 = CalculateValue(option, model, s, context.ValuationDate);
        double vSpotPlus = CalculateValue(option, model, s + ds, context.ValuationDate);
        double vSpotMinus = CalculateValue(option, model, s - ds, context.ValuationDate);

        return (vSpotPlus - 2 * v0 + vSpotMinus) / (ds * ds);
    }

    // DGammaDSpot
    public virtual double Speed(TOption option, PricingContext<TModel> context)
    {
        ValidateArguments(option, context);

        TModel model = context.ModelParameters;
        double s = context.AssetPrice;
        double ds = s * NumericalApproximationParameters.AssetPriceShiftFactor;

        double v0 = CalculateValue(option, model, s, context.ValuationDate);
        double vSpotPlus = CalculateValue(option, model, s + ds, context.ValuationDate);
        double vSpotPlusPlus = CalculateValue(option, model, s + 2 * ds, context.ValuationDate);
        double vSpotMinus = CalculateValue(option, model, s - ds, context.ValuationDate);

        return (vSpotPlusPlus - 3 * vSpotPlus + 3 * v0 - vSpotMinus) / (ds * ds * ds);
    }

    // DValueDTime (per day)
    public virtual double Theta(TOption option, PricingContext<TModel> context)
    {
        ValidateArguments(option, context);

        DateOnly t = context.ValuationDate;

        TModel model = context.ModelParameters;
        double v0 = CalculateValue(option, model, context.AssetPrice, context.ValuationDate);

        bool atExpiry = t >= option.ExpirationDate;
        double vNext = atExpiry ? 0 : CalculateValue(option, model, context.AssetPrice, t.AddDays(1));

        return vNext - v0;
    }

    // DDeltaDTime (per day)
    public virtual double Charm(TOption option, PricingContext<TModel> context)
    {
        ValidateArguments(option, context);

        TModel model = context.ModelParameters;
        double s = context.AssetPrice;
        DateOnly t = context.ValuationDate;
        double ds = s * NumericalApproximationParameters.AssetPriceShiftFactor;

        double vSpotPlus = CalculateValue(option, model, s + ds, t);
        double vSpotMinus = CalculateValue(option, model, s - ds, t);

        bool atExpiry = t >= option.ExpirationDate;
        double vSpotPlusNext = atExpiry ? 0 : CalculateValue(option, model, s + ds, t.AddDays(1));
        double vSpotMinusNext = atExpiry ? 0 : CalculateValue(option, model, s - ds, t.AddDays(1));

        return (vSpotPlusNext - vSpotPlus - vSpotMinusNext + vSpotMinus) / (2 * ds);
    }

    // DGammaDTime (per day)
    public virtual double Color(TOption option, PricingContext<TModel> context)
    {
        ValidateArguments(option, context);

        TModel model = context.ModelParameters;
        double s = context.AssetPrice;
        DateOnly t = context.ValuationDate;
        double ds = s * NumericalApproximationParameters.AssetPriceShiftFactor;

        double v0 = CalculateValue(option, model, s, t);
        double vSpotPlus = CalculateValue(option, model, s + ds, t);
        double vSpotMinus = CalculateValue(option, model, s - ds, t);

        bool atExpiry = t >= option.ExpirationDate;
        double vNext = atExpiry ? 0 : CalculateValue(option, model, s, t.AddDays(1));
        double vSpotPlusNext = atExpiry ? 0 : CalculateValue(option, model, s + ds, t.AddDays(1));
        double vSpotMinusNext = atExpiry ? 0 : CalculateValue(option, model, s - ds, t.AddDays(1));

        return (vSpotPlusNext - 2 * vNext + vSpotMinusNext - vSpotPlus + 2 * v0 - vSpotMinus) / (ds * ds);
    }

    public virtual double[] Values(TOption option, PricingContext<TModel> context, ReadOnlySpan<double> assetPrices)
    {
        Guard.IsGreaterThanOrEqualTo(assetPrices.Length, 3);

        double[] values = new double[assetPrices.Length];
        for (int i = 0; i < assetPrices.Length; i++)
        {
            values[i] = Value(option, context with { AssetPrice = assetPrices[i] });
        }

        return values;
    }

    public virtual double[] Deltas(TOption option, PricingContext<TModel> context, ReadOnlySpan<double> assetPrices)
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

    public virtual double[] Gammas(TOption option, PricingContext<TModel> context, ReadOnlySpan<double> assetPrices)
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

    protected abstract double CalculateValue(TOption option, TModel model, double assetPrice, DateOnly valuationDate);

    protected double GetYearsToExpiration(TOption option, DateOnly valuationDate)
    {
        Guard.IsLessThanOrEqualTo(valuationDate, option.ExpirationDate);

        return (option.ExpirationDate.DayNumber - valuationDate.DayNumber) / 365.0;
    }

    protected virtual void ValidateArguments(TOption option, PricingContext<TModel> context)
    {
        Guard.IsNotNull(option);
        Guard.IsBetweenOrEqualTo(context.ValuationDate, option.EffectiveDate, option.ExpirationDate);
    }
}
