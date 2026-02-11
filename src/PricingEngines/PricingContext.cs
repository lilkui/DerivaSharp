using DerivaSharp.Models;

namespace DerivaSharp.PricingEngines;

/// <summary>
///     Encapsulates the market context required for pricing an option.
/// </summary>
/// <typeparam name="TModelParameters">The type of model parameters.</typeparam>
/// <param name="ModelParameters">The model parameters.</param>
/// <param name="AssetPrice">The current asset price.</param>
/// <param name="ValuationDate">The valuation date.</param>
public readonly record struct PricingContext<TModelParameters>(TModelParameters ModelParameters, double AssetPrice, DateOnly ValuationDate)
    where TModelParameters : IModelParameters;
