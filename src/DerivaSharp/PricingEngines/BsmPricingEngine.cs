using DerivaSharp.Instruments;
using DerivaSharp.Models;
using MathNet.Numerics;
using MathNet.Numerics.RootFinding;

namespace DerivaSharp.PricingEngines;

public abstract class BsmPricingEngine<TOption> : PricingEngine<TOption, BsmModel>
    where TOption : Option
{
    // DValueDVol (per 1%)
    public virtual double Vega(TOption option, BsmModel model, MarketData market, PricingContext context)
    {
        ValidateArguments(option, context);

        double vol = model.Volatility;
        double dvol = NumericalApproximationParameters.VolatilityShift;

        double vVolPlus = CalculateValue(option, model with { Volatility = vol + dvol }, market, context);
        double vVolMinus = CalculateValue(option, model with { Volatility = vol - dvol }, market, context);

        return (vVolPlus - vVolMinus) / (2 * dvol) / 100;
    }

    // DDeltaDVol (per 1%)
    public virtual double Vanna(TOption option, BsmModel model, MarketData market, PricingContext context)
    {
        ValidateArguments(option, context);

        double s = market.AssetPrice;
        double vol = model.Volatility;

        double ds = s * NumericalApproximationParameters.AssetPriceShiftFactor;
        double dvol = NumericalApproximationParameters.VolatilityShift;

        double vSpotPlusVolPlus = CalculateValue(option, model with { Volatility = vol + dvol }, market with { AssetPrice = s + ds }, context);
        double vSpotMinusVolPlus = CalculateValue(option, model with { Volatility = vol + dvol }, market with { AssetPrice = s - ds }, context);
        double vSpotPlusVolMinus = CalculateValue(option, model with { Volatility = vol - dvol }, market with { AssetPrice = s + ds }, context);
        double vSpotMinusVolMinus = CalculateValue(option, model with { Volatility = vol - dvol }, market with { AssetPrice = s - ds }, context);

        return (vSpotPlusVolPlus - vSpotPlusVolMinus - vSpotMinusVolPlus + vSpotMinusVolMinus) / (4 * ds * dvol) / 100;
    }

    // DGammaDVol (per 1%)
    public virtual double Zomma(TOption option, BsmModel model, MarketData market, PricingContext context)
    {
        ValidateArguments(option, context);

        double s = market.AssetPrice;
        double vol = model.Volatility;

        double ds = s * NumericalApproximationParameters.AssetPriceShiftFactor;
        double dvol = NumericalApproximationParameters.VolatilityShift;

        double vVolPlus = CalculateValue(option, model with { Volatility = vol + dvol }, market, context);
        double vSpotPlusVolPlus = CalculateValue(option, model with { Volatility = vol + dvol }, market with { AssetPrice = s + ds }, context);
        double vSpotMinusVolPlus = CalculateValue(option, model with { Volatility = vol + dvol }, market with { AssetPrice = s - ds }, context);

        double vVolMinus = CalculateValue(option, model with { Volatility = vol - dvol }, market, context);
        double vSpotPlusVolMinus = CalculateValue(option, model with { Volatility = vol - dvol }, market with { AssetPrice = s + ds }, context);
        double vSpotMinusVolMinus = CalculateValue(option, model with { Volatility = vol - dvol }, market with { AssetPrice = s - ds }, context);

        return (vSpotPlusVolPlus - 2 * vVolPlus + vSpotMinusVolPlus - vSpotPlusVolMinus + 2 * vVolMinus - vSpotMinusVolMinus) / (2 * dvol * ds * ds) / 100;
    }

    // DValueDRate (per 1%)
    public virtual double Rho(TOption option, BsmModel model, MarketData market, PricingContext context)
    {
        ValidateArguments(option, context);

        double r = model.RiskFreeRate;
        double dr = NumericalApproximationParameters.InterestRateShift;

        double vRatePlus = CalculateValue(option, model with { RiskFreeRate = r + dr }, market, context);
        double vRateMinus = CalculateValue(option, model with { RiskFreeRate = r - dr }, market, context);

        return (vRatePlus - vRateMinus) / (2 * dr) / 100;
    }

    public virtual PricingResult ValueAndGreeks(TOption option, BsmModel model, MarketData market, PricingContext context)
    {
        ValidateArguments(option, context);

        double s = market.AssetPrice;
        DateOnly valuationDate = context.ValuationDate;
        double vol = model.Volatility;
        double r = model.RiskFreeRate;

        double ds = s * NumericalApproximationParameters.AssetPriceShiftFactor;
        double dvol = NumericalApproximationParameters.VolatilityShift;
        double dr = NumericalApproximationParameters.InterestRateShift;

        double v0 = CalculateValue(option, model, market, context);

        // Spot-shifted values
        double vSpotPlus = CalculateValue(option, model, market with { AssetPrice = s + ds }, context);
        double vSpotMinus = CalculateValue(option, model, market with { AssetPrice = s - ds }, context);
        double vSpotPlusPlus = CalculateValue(option, model, market with { AssetPrice = s + 2 * ds }, context);

        // Time-shifted values
        DateOnly nextDay = valuationDate.AddDays(1);
        bool atExpiry = valuationDate >= option.ExpirationDate;

        double vNext = atExpiry ? 0 : CalculateValue(option, model, market, context with { ValuationDate = nextDay });
        double vSpotPlusNext = atExpiry ? 0 : CalculateValue(option, model, market with { AssetPrice = s + ds }, context with { ValuationDate = nextDay });
        double vSpotMinusNext = atExpiry ? 0 : CalculateValue(option, model, market with { AssetPrice = s - ds }, context with { ValuationDate = nextDay });

        // Volatility-shifted values
        double vVolPlus = CalculateValue(option, model with { Volatility = vol + dvol }, market, context);
        double vVolMinus = CalculateValue(option, model with { Volatility = vol - dvol }, market, context);

        // Spot-volatility cross-shifted values
        double vSpotPlusVolPlus = CalculateValue(option, model with { Volatility = vol + dvol }, market with { AssetPrice = s + ds }, context);
        double vSpotMinusVolPlus = CalculateValue(option, model with { Volatility = vol + dvol }, market with { AssetPrice = s - ds }, context);
        double vSpotPlusVolMinus = CalculateValue(option, model with { Volatility = vol - dvol }, market with { AssetPrice = s + ds }, context);
        double vSpotMinusVolMinus = CalculateValue(option, model with { Volatility = vol - dvol }, market with { AssetPrice = s - ds }, context);

        // Rate-shifted values
        double vRatePlus = CalculateValue(option, model with { RiskFreeRate = r + dr }, market, context);
        double vRateMinus = CalculateValue(option, model with { RiskFreeRate = r - dr }, market, context);

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
        BsmModel model,
        MarketData market,
        PricingContext context,
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
            CalculateValue(option, model with { Volatility = vol }, market, context) - optionPrice;
    }
}
