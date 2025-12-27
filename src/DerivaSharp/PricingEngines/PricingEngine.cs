using System.Diagnostics;
using CommunityToolkit.Diagnostics;
using DerivaSharp.Instruments;
using DerivaSharp.Models;

namespace DerivaSharp.PricingEngines;

public abstract class PricingEngine<TOption, TModel>
    where TOption : Option
    where TModel : IModel
{
    public double Value(TOption option, TModel model, MarketData market, PricingContext context)
    {
        ValidateArguments(option, context);

        return CalculateValue(option, model, market, context);
    }

    // DValueDSpot
    public virtual double Delta(TOption option, TModel model, MarketData market, PricingContext context)
    {
        ValidateArguments(option, context);

        double s = market.AssetPrice;
        double ds = s * NumericalApproximationParameters.AssetPriceShiftFactor;

        double vSpotPlus = CalculateValue(option, model, market with { AssetPrice = s + ds }, context);
        double vSpotMinus = CalculateValue(option, model, market with { AssetPrice = s - ds }, context);

        return (vSpotPlus - vSpotMinus) / (2 * ds);
    }

    // DDeltaDSpot
    public virtual double Gamma(TOption option, TModel model, MarketData market, PricingContext context)
    {
        ValidateArguments(option, context);

        double s = market.AssetPrice;
        double ds = s * NumericalApproximationParameters.AssetPriceShiftFactor;

        double v0 = CalculateValue(option, model, market, context);
        double vSpotPlus = CalculateValue(option, model, market with { AssetPrice = s + ds }, context);
        double vSpotMinus = CalculateValue(option, model, market with { AssetPrice = s - ds }, context);

        return (vSpotPlus - 2 * v0 + vSpotMinus) / (ds * ds);
    }

    // DGammaDSpot
    public virtual double Speed(TOption option, TModel model, MarketData market, PricingContext context)
    {
        ValidateArguments(option, context);

        double s = market.AssetPrice;
        double ds = s * NumericalApproximationParameters.AssetPriceShiftFactor;

        double v0 = CalculateValue(option, model, market, context);
        double vSpotPlus = CalculateValue(option, model, market with { AssetPrice = s + ds }, context);
        double vSpotPlusPlus = CalculateValue(option, model, market with { AssetPrice = s + 2 * ds }, context);
        double vSpotMinus = CalculateValue(option, model, market with { AssetPrice = s - ds }, context);

        return (vSpotPlusPlus - 3 * vSpotPlus + 3 * v0 - vSpotMinus) / (ds * ds * ds);
    }

    // DValueDTime (per day)
    public virtual double Theta(TOption option, TModel model, MarketData market, PricingContext context)
    {
        ValidateArguments(option, context);

        DateOnly t = context.ValuationDate;

        double v0 = CalculateValue(option, model, market, context);

        bool atExpiry = t >= option.ExpirationDate;
        double vNext = atExpiry ? 0 : CalculateValue(option, model, market, context with { ValuationDate = t.AddDays(1) });

        return vNext - v0;
    }

    // DDeltaDTime (per day)
    public virtual double Charm(TOption option, TModel model, MarketData market, PricingContext context)
    {
        ValidateArguments(option, context);

        double s = market.AssetPrice;
        DateOnly t = context.ValuationDate;
        double ds = s * NumericalApproximationParameters.AssetPriceShiftFactor;

        double vSpotPlus = CalculateValue(option, model, market with { AssetPrice = s + ds }, context);
        double vSpotMinus = CalculateValue(option, model, market with { AssetPrice = s - ds }, context);

        bool atExpiry = t >= option.ExpirationDate;
        double vSpotPlusNext = atExpiry ? 0 : CalculateValue(option, model, market with { AssetPrice = s + ds }, context with { ValuationDate = t.AddDays(1) });
        double vSpotMinusNext = atExpiry ? 0 : CalculateValue(option, model, market with { AssetPrice = s - ds }, context with { ValuationDate = t.AddDays(1) });

        return (vSpotPlusNext - vSpotPlus - vSpotMinusNext + vSpotMinus) / (2 * ds);
    }

    // DGammaDTime (per day)
    public virtual double Color(TOption option, TModel model, MarketData market, PricingContext context)
    {
        ValidateArguments(option, context);

        double s = market.AssetPrice;
        DateOnly t = context.ValuationDate;
        double ds = s * NumericalApproximationParameters.AssetPriceShiftFactor;

        double v0 = CalculateValue(option, model, market, context);
        double vSpotPlus = CalculateValue(option, model, market with { AssetPrice = s + ds }, context);
        double vSpotMinus = CalculateValue(option, model, market with { AssetPrice = s - ds }, context);

        bool atExpiry = t >= option.ExpirationDate;
        double vNext = atExpiry ? 0 : CalculateValue(option, model, market, context with { ValuationDate = t.AddDays(1) });
        double vSpotPlusNext = atExpiry ? 0 : CalculateValue(option, model, market with { AssetPrice = s + ds }, context with { ValuationDate = t.AddDays(1) });
        double vSpotMinusNext = atExpiry ? 0 : CalculateValue(option, model, market with { AssetPrice = s - ds }, context with { ValuationDate = t.AddDays(1) });

        return (vSpotPlusNext - 2 * vNext + vSpotMinusNext - vSpotPlus + 2 * v0 - vSpotMinus) / (ds * ds);
    }

    protected abstract double CalculateValue(TOption option, TModel model, MarketData market, PricingContext context);

    protected double GetYearsToExpiration(TOption option, PricingContext context)
    {
        Debug.Assert(context.ValuationDate <= option.ExpirationDate);

        return (option.ExpirationDate.DayNumber - context.ValuationDate.DayNumber) / 365.0;
    }

    protected virtual void ValidateArguments(TOption option, PricingContext context)
    {
        Guard.IsNotNull(option);
        Guard.IsBetweenOrEqualTo(context.ValuationDate, option.EffectiveDate, option.ExpirationDate);
    }
}
