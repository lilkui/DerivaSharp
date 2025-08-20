namespace DerivaSharp.PricingEngines;

public readonly record struct PricingResult(
    double Value,
    double Delta,
    double Gamma,
    double Speed,
    double Theta,
    double Charm,
    double Color,
    double Vega,
    double Vanna,
    double Zomma,
    double Rho);
