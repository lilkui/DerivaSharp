namespace DerivaSharp.Instruments;

/// <summary>
///     Represents an Asian option with payoff based on the geometric average of the underlying asset price.
/// </summary>
public sealed record GeometricAverageOption : AsianOption
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="GeometricAverageOption" /> class.
    /// </summary>
    /// <param name="optionType">One of the <see cref="OptionType" /> enumeration values that specifies whether the option is a call or a put.</param>
    /// <param name="strikePrice">The strike price.</param>
    /// <param name="effectiveDate">The date when the option becomes effective.</param>
    /// <param name="expirationDate">The date when the option expires.</param>
    public GeometricAverageOption(OptionType optionType, double strikePrice, DateOnly effectiveDate, DateOnly expirationDate)
        : base(optionType, strikePrice, effectiveDate, expirationDate)
    {
    }
}
