namespace DerivaSharp.Instruments;

public sealed record EuropeanOption : VanillaOption
{
    public EuropeanOption(OptionType optionType, double strikePrice, DateOnly effectiveDate, DateOnly expirationDate)
        : base(optionType, Exercise.European, strikePrice, effectiveDate, expirationDate)
    {
    }
}
