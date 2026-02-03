namespace DerivaSharp.Instruments;

public abstract record AsianOption : StrikedTypePayoffOption
{
    protected AsianOption(OptionType optionType, double strikePrice, DateOnly effectiveDate, DateOnly expirationDate)
        : base(optionType, strikePrice, effectiveDate, expirationDate)
    {
    }
}
