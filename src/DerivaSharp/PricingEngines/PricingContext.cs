using CommunityToolkit.Diagnostics;

namespace DerivaSharp.PricingEngines;

public readonly record struct PricingContext
{
    public PricingContext(double assetPrice, DateOnly valuationDate, double volatility, double riskFreeRate, double dividendYield)
    {
        Guard.IsGreaterThanOrEqualTo(assetPrice, 0);
        Guard.IsGreaterThanOrEqualTo(volatility, 0);

        AssetPrice = assetPrice;
        ValuationDate = valuationDate;
        Volatility = volatility;
        RiskFreeRate = riskFreeRate;
        DividendYield = dividendYield;
    }

    public double AssetPrice { get; init; }

    public DateOnly ValuationDate { get; init; }

    public double Volatility { get; init; }

    public double RiskFreeRate { get; init; }

    public double DividendYield { get; init; }
}
