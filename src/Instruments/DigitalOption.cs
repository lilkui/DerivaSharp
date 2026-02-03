namespace DerivaSharp.Instruments;

public abstract record DigitalOption : StrikedTypePayoffOption
{
    protected DigitalOption(OptionType optionType, double strikePrice, DateOnly effectiveDate, DateOnly expirationDate)
        : base(optionType, strikePrice, effectiveDate, expirationDate)
    {
    }
}
