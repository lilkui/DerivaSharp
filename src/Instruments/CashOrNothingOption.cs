namespace DerivaSharp.Instruments;

/// <summary>
///     Represents a digital option that pays a fixed cash amount if the option expires in the money.
/// </summary>
public sealed record CashOrNothingOption : DigitalOption
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="CashOrNothingOption" /> class.
    /// </summary>
    /// <param name="optionType">The option type (call or put).</param>
    /// <param name="strikePrice">The strike price.</param>
    /// <param name="rebate">The fixed cash amount paid if the option expires in the money.</param>
    /// <param name="effectiveDate">The date when the option becomes effective.</param>
    /// <param name="expirationDate">The date when the option expires.</param>
    public CashOrNothingOption(OptionType optionType, double strikePrice, double rebate, DateOnly effectiveDate, DateOnly expirationDate)
        : base(optionType, strikePrice, effectiveDate, expirationDate) =>
        Rebate = rebate;

    /// <summary>
    ///     Gets the fixed cash amount paid if the option expires in the money.
    /// </summary>
    public double Rebate { get; init; }
}
