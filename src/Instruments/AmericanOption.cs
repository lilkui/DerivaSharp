namespace DerivaSharp.Instruments;

/// <summary>
///     Represents an American-style vanilla option that can be exercised at any time before expiration.
/// </summary>
public sealed record AmericanOption : VanillaOption
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="AmericanOption" /> class.
    /// </summary>
    /// <param name="optionType">The option type (call or put).</param>
    /// <param name="strikePrice">The strike price.</param>
    /// <param name="effectiveDate">The date when the option becomes effective.</param>
    /// <param name="expirationDate">The date when the option expires.</param>
    public AmericanOption(OptionType optionType, double strikePrice, DateOnly effectiveDate, DateOnly expirationDate)
        : base(optionType, Exercise.American, strikePrice, effectiveDate, expirationDate)
    {
    }
}
