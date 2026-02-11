namespace DerivaSharp.Instruments;

/// <summary>
///     Base class for Asian options whose payoff depends on the average price of the underlying asset.
/// </summary>
public abstract record AsianOption : StrikedTypePayoffOption
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="AsianOption" /> class.
    /// </summary>
    /// <param name="optionType">The option type (call or put).</param>
    /// <param name="strikePrice">The strike price.</param>
    /// <param name="effectiveDate">The date when the option becomes effective.</param>
    /// <param name="expirationDate">The date when the option expires.</param>
    protected AsianOption(OptionType optionType, double strikePrice, DateOnly effectiveDate, DateOnly expirationDate)
        : base(optionType, strikePrice, effectiveDate, expirationDate)
    {
    }
}
