using DerivaSharp.Instruments;
using DerivaSharp.Models;
using MathNet.Numerics;
using MathNet.Numerics.RootFinding;

namespace DerivaSharp.PricingEngines;

public abstract class BsmPricingEngine<TOption> : PricingEngine<TOption, BsmModel>
    where TOption : Option
{
    // DValueDVol (per 1%)
    public virtual double Vega(TOption option, PricingContext<BsmModel> context)
    {
        ValidateArguments(option, context);

        BsmModel model = context.Model;
        double assetPrice = context.AssetPrice;
        DateOnly valuationDate = context.ValuationDate;
        double vol = model.Volatility;
        double dvol = NumericalApproximationParameters.VolatilityShift;

        double vVolPlus = CalculateValue(option, model with { Volatility = vol + dvol }, assetPrice, valuationDate);
        double vVolMinus = CalculateValue(option, model with { Volatility = vol - dvol }, assetPrice, valuationDate);

        return (vVolPlus - vVolMinus) / (2 * dvol) / 100;
    }

    // DDeltaDVol (per 1%)
    public virtual double Vanna(TOption option, PricingContext<BsmModel> context)
    {
        ValidateArguments(option, context);

        BsmModel model = context.Model;
        double assetPrice = context.AssetPrice;
        DateOnly valuationDate = context.ValuationDate;
        double vol = model.Volatility;

        double ds = assetPrice * NumericalApproximationParameters.AssetPriceShiftFactor;
        double dvol = NumericalApproximationParameters.VolatilityShift;

        double vSpotPlusVolPlus = CalculateValue(option, model with { Volatility = vol + dvol }, assetPrice + ds, valuationDate);
        double vSpotMinusVolPlus = CalculateValue(option, model with { Volatility = vol + dvol }, assetPrice - ds, valuationDate);
        double vSpotPlusVolMinus = CalculateValue(option, model with { Volatility = vol - dvol }, assetPrice + ds, valuationDate);
        double vSpotMinusVolMinus = CalculateValue(option, model with { Volatility = vol - dvol }, assetPrice - ds, valuationDate);

        return (vSpotPlusVolPlus - vSpotPlusVolMinus - vSpotMinusVolPlus + vSpotMinusVolMinus) / (4 * ds * dvol) / 100;
    }

    // DGammaDVol (per 1%)
    public virtual double Zomma(TOption option, PricingContext<BsmModel> context)
    {
        ValidateArguments(option, context);

        BsmModel model = context.Model;
        double assetPrice = context.AssetPrice;
        DateOnly valuationDate = context.ValuationDate;
        double vol = model.Volatility;

        double ds = assetPrice * NumericalApproximationParameters.AssetPriceShiftFactor;
        double dvol = NumericalApproximationParameters.VolatilityShift;

        double vVolPlus = CalculateValue(option, model with { Volatility = vol + dvol }, assetPrice, valuationDate);
        double vSpotPlusVolPlus = CalculateValue(option, model with { Volatility = vol + dvol }, assetPrice + ds, valuationDate);
        double vSpotMinusVolPlus = CalculateValue(option, model with { Volatility = vol + dvol }, assetPrice - ds, valuationDate);

        double vVolMinus = CalculateValue(option, model with { Volatility = vol - dvol }, assetPrice, valuationDate);
        double vSpotPlusVolMinus = CalculateValue(option, model with { Volatility = vol - dvol }, assetPrice + ds, valuationDate);
        double vSpotMinusVolMinus = CalculateValue(option, model with { Volatility = vol - dvol }, assetPrice - ds, valuationDate);

        return (vSpotPlusVolPlus - 2 * vVolPlus + vSpotMinusVolPlus - vSpotPlusVolMinus + 2 * vVolMinus - vSpotMinusVolMinus) / (2 * dvol * ds * ds) / 100;
    }

    // DValueDRate (per 1%)
    public virtual double Rho(TOption option, PricingContext<BsmModel> context)
    {
        ValidateArguments(option, context);

        BsmModel model = context.Model;
        double assetPrice = context.AssetPrice;
        DateOnly valuationDate = context.ValuationDate;
        double r = model.RiskFreeRate;
        double dr = NumericalApproximationParameters.InterestRateShift;

        double vRatePlus = CalculateValue(option, model with { RiskFreeRate = r + dr }, assetPrice, valuationDate);
        double vRateMinus = CalculateValue(option, model with { RiskFreeRate = r - dr }, assetPrice, valuationDate);

        return (vRatePlus - vRateMinus) / (2 * dr) / 100;
    }

    public virtual PricingResult ValueAndGreeks(TOption option, PricingContext<BsmModel> context)
    {
        ValidateArguments(option, context);

        BsmModel model = context.Model;
        double assetPrice = context.AssetPrice;
        DateOnly valuationDate = context.ValuationDate;
        double vol = model.Volatility;
        double r = model.RiskFreeRate;

        double ds = assetPrice * NumericalApproximationParameters.AssetPriceShiftFactor;
        double dvol = NumericalApproximationParameters.VolatilityShift;
        double dr = NumericalApproximationParameters.InterestRateShift;

        double v0 = CalculateValue(option, model, assetPrice, valuationDate);

        // Spot-shifted values
        double vSpotPlus = CalculateValue(option, model, assetPrice + ds, valuationDate);
        double vSpotMinus = CalculateValue(option, model, assetPrice - ds, valuationDate);
        double vSpotPlusPlus = CalculateValue(option, model, assetPrice + 2 * ds, valuationDate);

        // Time-shifted values
        DateOnly nextDay = valuationDate.AddDays(1);
        bool atExpiry = valuationDate >= option.ExpirationDate;

        double vNext = atExpiry ? 0 : CalculateValue(option, model, assetPrice, nextDay);
        double vSpotPlusNext = atExpiry ? 0 : CalculateValue(option, model, assetPrice + ds, nextDay);
        double vSpotMinusNext = atExpiry ? 0 : CalculateValue(option, model, assetPrice - ds, nextDay);

        // Volatility-shifted values
        double vVolPlus = CalculateValue(option, model with { Volatility = vol + dvol }, assetPrice, valuationDate);
        double vVolMinus = CalculateValue(option, model with { Volatility = vol - dvol }, assetPrice, valuationDate);

        // Spot-volatility cross-shifted values
        double vSpotPlusVolPlus = CalculateValue(option, model with { Volatility = vol + dvol }, assetPrice + ds, valuationDate);
        double vSpotMinusVolPlus = CalculateValue(option, model with { Volatility = vol + dvol }, assetPrice - ds, valuationDate);
        double vSpotPlusVolMinus = CalculateValue(option, model with { Volatility = vol - dvol }, assetPrice + ds, valuationDate);
        double vSpotMinusVolMinus = CalculateValue(option, model with { Volatility = vol - dvol }, assetPrice - ds, valuationDate);

        // Rate-shifted values
        double vRatePlus = CalculateValue(option, model with { RiskFreeRate = r + dr }, assetPrice, valuationDate);
        double vRateMinus = CalculateValue(option, model with { RiskFreeRate = r - dr }, assetPrice, valuationDate);

        double delta = (vSpotPlus - vSpotMinus) / (2 * ds);
        double gamma = (vSpotPlus - 2 * v0 + vSpotMinus) / (ds * ds);
        double speed = (vSpotPlusPlus - 3 * vSpotPlus + 3 * v0 - vSpotMinus) / (ds * ds * ds);

        double theta = vNext - v0;
        double charm = (vSpotPlusNext - vSpotPlus - vSpotMinusNext + vSpotMinus) / (2 * ds);
        double color = (vSpotPlusNext - 2 * vNext + vSpotMinusNext - (vSpotPlus - 2 * v0 + vSpotMinus)) / (ds * ds);

        double vega = (vVolPlus - vVolMinus) / (2 * dvol) / 100;
        double vanna = (vSpotPlusVolPlus - vSpotPlusVolMinus - vSpotMinusVolPlus + vSpotMinusVolMinus) / (4 * ds * dvol) / 100;
        double zomma = (vSpotPlusVolPlus - 2 * vVolPlus + vSpotMinusVolPlus - (vSpotPlusVolMinus - 2 * vVolMinus + vSpotMinusVolMinus)) / (2 * dvol * ds * ds) / 100;

        double rho = (vRatePlus - vRateMinus) / (2 * dr) / 100;

        return new PricingResult(v0, delta, gamma, speed, theta, charm, color, vega, vanna, zomma, rho);
    }

    public virtual double ImpliedVolatility(
        TOption option,
        PricingContext<BsmModel> context,
        double optionPrice,
        double lowerBound = 0.0001,
        double upperBound = 4.0,
        double accuracy = 1e-5)
    {
        ValidateArguments(option, context);

        try
        {
            return Brent.FindRoot(ObjectiveFunction, lowerBound, upperBound, accuracy);
        }
        catch (NonConvergenceException)
        {
            return double.NaN;
        }

        double ObjectiveFunction(double vol) =>
            CalculateValue(option, context.Model with { Volatility = vol }, context.AssetPrice, context.ValuationDate) - optionPrice;
    }
}
