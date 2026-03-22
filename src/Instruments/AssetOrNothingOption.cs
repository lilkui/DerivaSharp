namespace DerivaSharp.Instruments;

/// <summary>
///     Represents a digital option that pays the asset price if the option expires in the money.
/// </summary>
public sealed record AssetOrNothingOption : DigitalOption
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="AssetOrNothingOption" /> class.
    /// </summary>
    /// <param name="optionType">One of the <see cref="OptionType" /> enumeration values that specifies whether the option is a call or a put.</param>
    /// <param name="strikePrice">The strike price.</param>
    /// <param name="effectiveDate">The date when the option becomes effective.</param>
    /// <param name="expirationDate">The date when the option expires.</param>
    public AssetOrNothingOption(OptionType optionType, double strikePrice, DateOnly effectiveDate, DateOnly expirationDate)
        : base(optionType, strikePrice, effectiveDate, expirationDate)
    {
    }
}
