using CommunityToolkit.Diagnostics;

namespace DerivaSharp.Models;

public readonly record struct BsmModel : IModel
{
    public BsmModel(double volatility, double riskFreeRate, double dividendYield)
    {
        Guard.IsGreaterThanOrEqualTo(volatility, 0);

        Volatility = volatility;
        RiskFreeRate = riskFreeRate;
        DividendYield = dividendYield;
    }

    public double Volatility { get; init; }

    public double RiskFreeRate { get; init; }

    public double DividendYield { get; init; }
}
