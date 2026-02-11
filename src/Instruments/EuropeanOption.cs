namespace DerivaSharp.Instruments;

/// <summary>
///     Represents a European-style vanilla option that can only be exercised at expiration.
/// </summary>
public sealed record EuropeanOption : VanillaOption
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="EuropeanOption" /> class.
    /// </summary>
    /// <param name="optionType">The option type (call or put).</param>
    /// <param name="strikePrice">The strike price.</param>
    /// <param name="effectiveDate">The date when the option becomes effective.</param>
    /// <param name="expirationDate">The date when the option expires.</param>
    public EuropeanOption(OptionType optionType, double strikePrice, DateOnly effectiveDate, DateOnly expirationDate)
        : base(optionType, Exercise.European, strikePrice, effectiveDate, expirationDate)
    {
    }
}
