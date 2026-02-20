using DerivaSharp.Instruments;
using DerivaSharp.Models;
using DerivaSharp.Numerics;

namespace DerivaSharp.PricingEngines;

/// <summary>
///     Base class for pricing engines using the Black-Scholes-Merton model.
/// </summary>
/// <typeparam name="TOption">The type of option to price.</typeparam>
public abstract class BsmPricingEngine<TOption> : PricingEngine<TOption, BsmModelParameters>
    where TOption : Option
{
    /// <summary>
    ///     Computes vega using numerical differentiation.
    /// </summary>
    /// <param name="option">The option to price.</param>
    /// <param name="context">The pricing context.</param>
    /// <returns>The rate of change of value with respect to volatility (per 1%).</returns>
    public virtual double Vega(TOption option, PricingContext<BsmModelParameters> context)
    {
        ValidateArguments(option, context);

        BsmModelParameters parameters = context.ModelParameters;
        double vol = parameters.Volatility;
        double dvol = ShiftParameters.VolatilityShift;

        double vVolPlus = CalculateValue(option, context with { ModelParameters = parameters with { Volatility = vol + dvol } });
        double vVolMinus = CalculateValue(option, context with { ModelParameters = parameters with { Volatility = vol - dvol } });

        return (vVolPlus - vVolMinus) / (2 * dvol) / 100;
    }

    /// <summary>
    ///     Computes vanna using numerical differentiation.
    /// </summary>
    /// <param name="option">The option to price.</param>
    /// <param name="context">The pricing context.</param>
    /// <returns>The rate of change of delta with respect to volatility (per 1%).</returns>
    public virtual double Vanna(TOption option, PricingContext<BsmModelParameters> context)
    {
        ValidateArguments(option, context);

        BsmModelParameters parameters = context.ModelParameters;
        double assetPrice = context.AssetPrice;
        double vol = parameters.Volatility;

        double ds = assetPrice * ShiftParameters.AssetPriceShiftFactor;
        double dvol = ShiftParameters.VolatilityShift;

        double vSpotPlusVolPlus = CalculateValue(option, context with
        {
            ModelParameters = parameters with { Volatility = vol + dvol },
            AssetPrice = assetPrice + ds,
        });
        double vSpotMinusVolPlus = CalculateValue(option, context with
        {
            ModelParameters = parameters with { Volatility = vol + dvol },
            AssetPrice = assetPrice - ds,
        });
        double vSpotPlusVolMinus = CalculateValue(option, context with
        {
            ModelParameters = parameters with { Volatility = vol - dvol },
            AssetPrice = assetPrice + ds,
        });
        double vSpotMinusVolMinus = CalculateValue(option, context with
        {
            ModelParameters = parameters with { Volatility = vol - dvol },
            AssetPrice = assetPrice - ds,
        });

        return (vSpotPlusVolPlus - vSpotPlusVolMinus - vSpotMinusVolPlus + vSpotMinusVolMinus) / (4 * ds * dvol) / 100;
    }

    /// <summary>
    ///     Computes zomma using numerical differentiation.
    /// </summary>
    /// <param name="option">The option to price.</param>
    /// <param name="context">The pricing context.</param>
    /// <returns>The rate of change of gamma with respect to volatility (per 1%).</returns>
    public virtual double Zomma(TOption option, PricingContext<BsmModelParameters> context)
    {
        ValidateArguments(option, context);

        BsmModelParameters parameters = context.ModelParameters;
        double assetPrice = context.AssetPrice;
        double vol = parameters.Volatility;

        double ds = assetPrice * ShiftParameters.AssetPriceShiftFactor;
        double dvol = ShiftParameters.VolatilityShift;

        double vVolPlus = CalculateValue(option, context with { ModelParameters = parameters with { Volatility = vol + dvol } });
        double vSpotPlusVolPlus = CalculateValue(option, context with
        {
            ModelParameters = parameters with { Volatility = vol + dvol },
            AssetPrice = assetPrice + ds,
        });
        double vSpotMinusVolPlus = CalculateValue(option, context with
        {
            ModelParameters = parameters with { Volatility = vol + dvol },
            AssetPrice = assetPrice - ds,
        });

        double vVolMinus = CalculateValue(option, context with { ModelParameters = parameters with { Volatility = vol - dvol } });
        double vSpotPlusVolMinus = CalculateValue(option, context with
        {
            ModelParameters = parameters with { Volatility = vol - dvol },
            AssetPrice = assetPrice + ds,
        });
        double vSpotMinusVolMinus = CalculateValue(option, context with
        {
            ModelParameters = parameters with { Volatility = vol - dvol },
            AssetPrice = assetPrice - ds,
        });

        return (vSpotPlusVolPlus - 2 * vVolPlus + vSpotMinusVolPlus - vSpotPlusVolMinus + 2 * vVolMinus - vSpotMinusVolMinus) / (2 * dvol * ds * ds) / 100;
    }

    /// <summary>
    ///     Computes rho using numerical differentiation.
    /// </summary>
    /// <param name="option">The option to price.</param>
    /// <param name="context">The pricing context.</param>
    /// <returns>The rate of change of value with respect to the risk-free rate (per 1%).</returns>
    public virtual double Rho(TOption option, PricingContext<BsmModelParameters> context)
    {
        ValidateArguments(option, context);

        BsmModelParameters parameters = context.ModelParameters;
        double r = parameters.RiskFreeRate;
        double dr = ShiftParameters.InterestRateShift;

        double vRatePlus = CalculateValue(option, context with { ModelParameters = parameters with { RiskFreeRate = r + dr } });
        double vRateMinus = CalculateValue(option, context with { ModelParameters = parameters with { RiskFreeRate = r - dr } });

        return (vRatePlus - vRateMinus) / (2 * dr) / 100;
    }

    /// <summary>
    ///     Computes the option value and all Greeks using numerical differentiation.
    /// </summary>
    /// <param name="option">The option to price.</param>
    /// <param name="context">The pricing context.</param>
    /// <returns>A <see cref="PricingResult" /> containing the value and Greeks.</returns>
    public virtual PricingResult ValueAndGreeks(TOption option, PricingContext<BsmModelParameters> context)
    {
        ValidateArguments(option, context);

        BsmModelParameters parameters = context.ModelParameters;
        double assetPrice = context.AssetPrice;
        DateOnly valuationDate = context.ValuationDate;
        double vol = parameters.Volatility;
        double r = parameters.RiskFreeRate;

        double ds = assetPrice * ShiftParameters.AssetPriceShiftFactor;
        double dvol = ShiftParameters.VolatilityShift;
        double dr = ShiftParameters.InterestRateShift;

        double v0 = CalculateValue(option, context);

        // Spot-shifted values
        double vSpotPlus = CalculateValue(option, context with { AssetPrice = assetPrice + ds });
        double vSpotMinus = CalculateValue(option, context with { AssetPrice = assetPrice - ds });
        double vSpotPlusPlus = CalculateValue(option, context with { AssetPrice = assetPrice + 2 * ds });

        // Time-shifted values
        DateOnly nextDay = valuationDate.AddDays(1);
        bool atExpiry = valuationDate >= option.ExpirationDate;

        double vNext = atExpiry ? 0 : CalculateValue(option, context with { ValuationDate = nextDay });
        double vSpotPlusNext = atExpiry
            ? 0
            : CalculateValue(option, context with
            {
                AssetPrice = assetPrice + ds,
                ValuationDate = nextDay,
            });
        double vSpotMinusNext = atExpiry
            ? 0
            : CalculateValue(option, context with
            {
                AssetPrice = assetPrice - ds,
                ValuationDate = nextDay,
            });

        // Volatility-shifted values
        double vVolPlus = CalculateValue(option, context with { ModelParameters = parameters with { Volatility = vol + dvol } });
        double vVolMinus = CalculateValue(option, context with { ModelParameters = parameters with { Volatility = vol - dvol } });

        // Spot-volatility cross-shifted values
        double vSpotPlusVolPlus = CalculateValue(option, context with
        {
            ModelParameters = parameters with { Volatility = vol + dvol },
            AssetPrice = assetPrice + ds,
        });
        double vSpotMinusVolPlus = CalculateValue(option, context with
        {
            ModelParameters = parameters with { Volatility = vol + dvol },
            AssetPrice = assetPrice - ds,
        });
        double vSpotPlusVolMinus = CalculateValue(option, context with
        {
            ModelParameters = parameters with { Volatility = vol - dvol },
            AssetPrice = assetPrice + ds,
        });
        double vSpotMinusVolMinus = CalculateValue(option, context with
        {
            ModelParameters = parameters with { Volatility = vol - dvol },
            AssetPrice = assetPrice - ds,
        });

        // Rate-shifted values
        double vRatePlus = CalculateValue(option, context with { ModelParameters = parameters with { RiskFreeRate = r + dr } });
        double vRateMinus = CalculateValue(option, context with { ModelParameters = parameters with { RiskFreeRate = r - dr } });

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

    /// <summary>
    ///     Computes the implied volatility for a given option price using Brent's method.
    /// </summary>
    /// <param name="option">The option to price.</param>
    /// <param name="context">The pricing context.</param>
    /// <param name="optionPrice">The observed option price.</param>
    /// <param name="lowerBound">The lower bound for the volatility search.</param>
    /// <param name="upperBound">The upper bound for the volatility search.</param>
    /// <param name="accuracy">The desired accuracy.</param>
    /// <returns>The implied volatility, or <see cref="double.NaN" /> if the root-finding fails to converge.</returns>
    public virtual double ImpliedVolatility(
        TOption option,
        PricingContext<BsmModelParameters> context,
        double optionPrice,
        double lowerBound = 0.0001,
        double upperBound = 4.0,
        double accuracy = 1e-5)
    {
        ValidateArguments(option, context);

        if (BrentSolver.TryFindRoot(ObjectiveFunction, lowerBound, upperBound, accuracy, 100, out double root))
        {
            return root;
        }

        return double.NaN;

        double ObjectiveFunction(double vol) =>
            CalculateValue(option, context with { ModelParameters = context.ModelParameters with { Volatility = vol } }) - optionPrice;
    }
}
