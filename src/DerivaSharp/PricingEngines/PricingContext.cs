using DerivaSharp.Models;

namespace DerivaSharp.PricingEngines;

public readonly record struct PricingContext<TModel>(TModel Model, double AssetPrice, DateOnly ValuationDate)
    where TModel : IModel;
