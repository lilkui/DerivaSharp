namespace DerivaSharp.PricingEngines;

/// <summary>
///     Specifies the finite difference scheme for PDE discretization.
/// </summary>
public enum FiniteDifferenceScheme
{
    /// <summary>
    ///     Explicit Euler scheme (theta = 0).
    /// </summary>
    ExplicitEuler,

    /// <summary>
    ///     Implicit Euler scheme (theta = 1).
    /// </summary>
    ImplicitEuler,

    /// <summary>
    ///     Crank-Nicolson scheme (theta = 0.5).
    /// </summary>
    CrankNicolson,
}
