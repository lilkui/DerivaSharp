using CommunityToolkit.Diagnostics;

namespace DerivaSharp.Instruments;

/// <summary>
///     Base class for all option instruments.
/// </summary>
public abstract record Option
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="Option" /> class.
    /// </summary>
    /// <param name="effectiveDate">The date when the option becomes effective.</param>
    /// <param name="expirationDate">The date when the option expires.</param>
    protected Option(DateOnly effectiveDate, DateOnly expirationDate)
    {
        Guard.IsGreaterThanOrEqualTo(expirationDate, effectiveDate);

        EffectiveDate = effectiveDate;
        ExpirationDate = expirationDate;
    }

    /// <summary>
    ///     Gets the date when the option becomes effective.
    /// </summary>
    public DateOnly EffectiveDate { get; init; }

    /// <summary>
    ///     Gets the date when the option expires.
    /// </summary>
    public DateOnly ExpirationDate { get; init; }
}
