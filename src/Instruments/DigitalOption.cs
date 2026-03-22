namespace DerivaSharp.Instruments;

/// <summary>
///     Base class for digital (binary) options that pay a fixed amount or nothing.
/// </summary>
public abstract record DigitalOption : StrikedTypePayoffOption
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="DigitalOption" /> class.
    /// </summary>
    /// <param name="optionType">One of the <see cref="OptionType" /> enumeration values that specifies whether the option is a call or a put.</param>
    /// <param name="strikePrice">The strike price.</param>
    /// <param name="effectiveDate">The date when the option becomes effective.</param>
    /// <param name="expirationDate">The date when the option expires.</param>
    protected DigitalOption(OptionType optionType, double strikePrice, DateOnly effectiveDate, DateOnly expirationDate)
        : base(optionType, strikePrice, effectiveDate, expirationDate)
    {
    }
}
