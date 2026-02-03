namespace DerivaSharp.Instruments;

public sealed record AssetOrNothingOption : DigitalOption
{
    public AssetOrNothingOption(OptionType optionType, double strikePrice, DateOnly effectiveDate, DateOnly expirationDate)
        : base(optionType, strikePrice, effectiveDate, expirationDate)
    {
    }
}
