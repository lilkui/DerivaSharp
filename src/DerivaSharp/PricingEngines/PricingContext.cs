using DerivaSharp.Models;

namespace DerivaSharp.PricingEngines;

public readonly record struct PricingContext<TModelParameters>(TModelParameters ModelParameters, double AssetPrice, DateOnly ValuationDate)
    where TModelParameters : IModelParameters;
