using CommunityToolkit.Diagnostics;
using DerivaSharp.Instruments;
using MathNet.Numerics;
using MathNet.Numerics.RootFinding;

namespace DerivaSharp.PricingEngines;

public abstract class PricingEngine<TOption>
    where TOption : Option
{
    public double Value(TOption option, PricingContext context)
    {
        ValidateArguments(option, context);
        return CalculateValue(option, context);
    }

    // DValueDSpot
    public virtual double Delta(TOption option, PricingContext context)
    {
        ValidateArguments(option, context);
        double s = context.AssetPrice;
        double ds = s * NumericalApproximationParameters.AssetPriceShiftFactor;
        double vSpotPlus = CalculateValue(option, context with { AssetPrice = s + ds });
        double vSpotMinus = CalculateValue(option, context with { AssetPrice = s - ds });
        return (vSpotPlus - vSpotMinus) / (2 * ds);
    }

    // DDeltaDSpot
    public virtual double Gamma(TOption option, PricingContext context)
    {
        ValidateArguments(option, context);
        double s = context.AssetPrice;
        double ds = s * NumericalApproximationParameters.AssetPriceShiftFactor;
        double v0 = CalculateValue(option, context);
        double vSpotPlus = CalculateValue(option, context with { AssetPrice = s + ds });
        double vSpotMinus = CalculateValue(option, context with { AssetPrice = s - ds });
        return (vSpotPlus - 2 * v0 + vSpotMinus) / (ds * ds);
    }

    // DGammaDSpot
    public virtual double Speed(TOption option, PricingContext context)
    {
        ValidateArguments(option, context);
        double s = context.AssetPrice;
        double ds = s * NumericalApproximationParameters.AssetPriceShiftFactor;
        double v0 = CalculateValue(option, context);
        double vSpotPlus = CalculateValue(option, context with { AssetPrice = s + ds });
        double vSpotPlusPlus = CalculateValue(option, context with { AssetPrice = s + 2 * ds });
        double vSpotMinus = CalculateValue(option, context with { AssetPrice = s - ds });
        return (vSpotPlusPlus - 3 * vSpotPlus + 3 * v0 - vSpotMinus) / (ds * ds * ds);
    }

    // DValueDTime (per day)
    public virtual double Theta(TOption option, PricingContext context)
    {
        ValidateArguments(option, context);
        DateOnly t = context.ValuationDate;
        double v0 = CalculateValue(option, context);
        bool atExpiry = t >= option.ExpirationDate;
        double vNext = atExpiry ? 0 : CalculateValue(option, context with { ValuationDate = t.AddDays(1) });
        return vNext - v0;
    }

    // DDeltaDTime (per day)
    public virtual double Charm(TOption option, PricingContext context)
    {
        ValidateArguments(option, context);
        double s = context.AssetPrice;
        DateOnly t = context.ValuationDate;
        double ds = s * NumericalApproximationParameters.AssetPriceShiftFactor;
        double vSpotPlus = CalculateValue(option, context with { AssetPrice = s + ds });
        double vSpotMinus = CalculateValue(option, context with { AssetPrice = s - ds });
        bool atExpiry = t >= option.ExpirationDate;
        double vSpotPlusNext = atExpiry ? 0 : CalculateValue(option, context with { AssetPrice = s + ds, ValuationDate = t.AddDays(1) });
        double vSpotMinusNext = atExpiry ? 0 : CalculateValue(option, context with { AssetPrice = s - ds, ValuationDate = t.AddDays(1) });
        return (vSpotPlusNext - vSpotPlus - vSpotMinusNext + vSpotMinus) / (2 * ds);
    }

    // DGammaDTime (per day)
    public virtual double Color(TOption option, PricingContext context)
    {
        ValidateArguments(option, context);
        double s = context.AssetPrice;
        DateOnly t = context.ValuationDate;
        double ds = s * NumericalApproximationParameters.AssetPriceShiftFactor;
        double v0 = CalculateValue(option, context);
        double vSpotPlus = CalculateValue(option, context with { AssetPrice = s + ds });
        double vSpotMinus = CalculateValue(option, context with { AssetPrice = s - ds });
        bool atExpiry = t >= option.ExpirationDate;
        double vNext = atExpiry ? 0 : CalculateValue(option, context with { ValuationDate = t.AddDays(1) });
        double vSpotPlusNext = atExpiry ? 0 : CalculateValue(option, context with { AssetPrice = s + ds, ValuationDate = t.AddDays(1) });
        double vSpotMinusNext = atExpiry ? 0 : CalculateValue(option, context with { AssetPrice = s - ds, ValuationDate = t.AddDays(1) });
        return (vSpotPlusNext - 2 * vNext + vSpotMinusNext - vSpotPlus + 2 * v0 - vSpotMinus) / (ds * ds);
    }

    // DValueDVol (per 1%)
    public virtual double Vega(TOption option, PricingContext context)
    {
        ValidateArguments(option, context);
        double vol = context.Volatility;
        double dvol = NumericalApproximationParameters.VolatilityShift;
        double vVolPlus = CalculateValue(option, context with { Volatility = vol + dvol });
        double vVolMinus = CalculateValue(option, context with { Volatility = vol - dvol });
        return (vVolPlus - vVolMinus) / (2 * dvol) / 100;
    }

    // DDeltaDVol (per 1%)
    public virtual double Vanna(TOption option, PricingContext context)
    {
        ValidateArguments(option, context);
        double s = context.AssetPrice;
        double vol = context.Volatility;
        double ds = s * NumericalApproximationParameters.AssetPriceShiftFactor;
        double dvol = NumericalApproximationParameters.VolatilityShift;
        double vSpotPlusVolPlus = CalculateValue(option, context with { AssetPrice = s + ds, Volatility = vol + dvol });
        double vSpotMinusVolPlus = CalculateValue(option, context with { AssetPrice = s - ds, Volatility = vol + dvol });
        double vSpotPlusVolMinus = CalculateValue(option, context with { AssetPrice = s + ds, Volatility = vol - dvol });
        double vSpotMinusVolMinus = CalculateValue(option, context with { AssetPrice = s - ds, Volatility = vol - dvol });
        return (vSpotPlusVolPlus - vSpotPlusVolMinus - vSpotMinusVolPlus + vSpotMinusVolMinus) / (4 * ds * dvol) / 100;
    }

    // DGammaDVol (per 1%)
    public virtual double Zomma(TOption option, PricingContext context)
    {
        ValidateArguments(option, context);
        double s = context.AssetPrice;
        double vol = context.Volatility;
        double ds = s * NumericalApproximationParameters.AssetPriceShiftFactor;
        double dvol = NumericalApproximationParameters.VolatilityShift;
        double vVolPlus = CalculateValue(option, context with { Volatility = vol + dvol });
        double vSpotPlusVolPlus = CalculateValue(option, context with { AssetPrice = s + ds, Volatility = vol + dvol });
        double vSpotMinusVolPlus = CalculateValue(option, context with { AssetPrice = s - ds, Volatility = vol + dvol });
        double vVolMinus = CalculateValue(option, context with { Volatility = vol - dvol });
        double vSpotPlusVolMinus = CalculateValue(option, context with { AssetPrice = s + ds, Volatility = vol - dvol });
        double vSpotMinusVolMinus = CalculateValue(option, context with { AssetPrice = s - ds, Volatility = vol - dvol });
        return (vSpotPlusVolPlus - 2 * vVolPlus + vSpotMinusVolPlus - vSpotPlusVolMinus + 2 * vVolMinus - vSpotMinusVolMinus) / (2 * dvol * ds * ds) / 100;
    }

    // DValueDRate (per 1%)
    public virtual double Rho(TOption option, PricingContext context)
    {
        ValidateArguments(option, context);
        double r = context.RiskFreeRate;
        double dr = NumericalApproximationParameters.InterestRateShift;
        double vRatePlus = CalculateValue(option, context with { RiskFreeRate = r + dr });
        double vRateMinus = CalculateValue(option, context with { RiskFreeRate = r - dr });
        return (vRatePlus - vRateMinus) / (2 * dr) / 100;
    }

    public virtual PricingResult ValueAndGreeks(TOption option, PricingContext context)
    {
        ValidateArguments(option, context);

        double s = context.AssetPrice;
        DateOnly valuationDate = context.ValuationDate;
        double vol = context.Volatility;
        double r = context.RiskFreeRate;
        double ds = s * NumericalApproximationParameters.AssetPriceShiftFactor;
        double dvol = NumericalApproximationParameters.VolatilityShift;
        double dr = NumericalApproximationParameters.InterestRateShift;

        double v0 = CalculateValue(option, context);

        // Spot-shifted values
        double vSpotPlus = CalculateValue(option, context with { AssetPrice = s + ds });
        double vSpotMinus = CalculateValue(option, context with { AssetPrice = s - ds });
        double vSpotPlusPlus = CalculateValue(option, context with { AssetPrice = s + 2 * ds });

        // Time-shifted values
        DateOnly nextDay = valuationDate.AddDays(1);
        bool atExpiry = valuationDate >= option.ExpirationDate;
        double vNext = atExpiry ? 0 : CalculateValue(option, context with { ValuationDate = nextDay });
        double vSpotPlusNext = atExpiry ? 0 : CalculateValue(option, context with { AssetPrice = s + ds, ValuationDate = nextDay });
        double vSpotMinusNext = atExpiry ? 0 : CalculateValue(option, context with { AssetPrice = s - ds, ValuationDate = nextDay });

        // Volatility-shifted values
        double vVolPlus = CalculateValue(option, context with { Volatility = vol + dvol });
        double vVolMinus = CalculateValue(option, context with { Volatility = vol - dvol });

        // Spot-volatility cross-shifted values
        double vSpotPlusVolPlus = CalculateValue(option, context with { AssetPrice = s + ds, Volatility = vol + dvol });
        double vSpotMinusVolPlus = CalculateValue(option, context with { AssetPrice = s - ds, Volatility = vol + dvol });
        double vSpotPlusVolMinus = CalculateValue(option, context with { AssetPrice = s + ds, Volatility = vol - dvol });
        double vSpotMinusVolMinus = CalculateValue(option, context with { AssetPrice = s - ds, Volatility = vol - dvol });

        // Rate-shifted values
        double vRatePlus = CalculateValue(option, context with { RiskFreeRate = r + dr });
        double vRateMinus = CalculateValue(option, context with { RiskFreeRate = r - dr });

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

        double ObjectiveFunction(double vol)
        {
            return CalculateValue(option, context with { Volatility = vol }) - optionPrice;
        }
    }

    protected abstract double CalculateValue(TOption option, PricingContext context);

    protected double GetYearsToExpiration(TOption option, PricingContext context)
    {
        Guard.IsLessThanOrEqualTo(context.ValuationDate, option.ExpirationDate);
        return (option.ExpirationDate.DayNumber - context.ValuationDate.DayNumber) / 365.0;
    }

    protected virtual void ValidateArguments(TOption option, PricingContext context)
    {
        Guard.IsNotNull(option);
        Guard.IsBetweenOrEqualTo(context.ValuationDate, option.EffectiveDate, option.ExpirationDate);
    }
}
