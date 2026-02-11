using CommunityToolkit.Diagnostics;

namespace DerivaSharp.Instruments;

/// <summary>
///     Base class for options with a strike price and option type.
/// </summary>
public abstract record StrikedTypePayoffOption : Option
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="StrikedTypePayoffOption" /> class.
    /// </summary>
    /// <param name="optionType">The option type (call or put).</param>
    /// <param name="strikePrice">The strike price.</param>
    /// <param name="effectiveDate">The date when the option becomes effective.</param>
    /// <param name="expirationDate">The date when the option expires.</param>
    protected StrikedTypePayoffOption(OptionType optionType, double strikePrice, DateOnly effectiveDate, DateOnly expirationDate)
        : base(effectiveDate, expirationDate)
    {
        Guard.IsGreaterThan(strikePrice, 0);

        OptionType = optionType;
        StrikePrice = strikePrice;
    }

    /// <summary>
    ///     Gets the option type (call or put).
    /// </summary>
    public OptionType OptionType { get; init; }

    /// <summary>
    ///     Gets the strike price.
    /// </summary>
    public double StrikePrice { get; init; }
}
