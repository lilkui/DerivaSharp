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
