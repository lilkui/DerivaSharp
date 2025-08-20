namespace DerivaSharp.Instruments;

public sealed record AmericanOption : VanillaOption
{
    public AmericanOption(OptionType optionType, double strikePrice, DateOnly effectiveDate, DateOnly expirationDate)
        : base(optionType, Exercise.American, strikePrice, effectiveDate, expirationDate)
    {
    }
}
