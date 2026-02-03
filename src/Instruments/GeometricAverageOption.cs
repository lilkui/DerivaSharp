namespace DerivaSharp.Instruments;

public sealed record GeometricAverageOption : AsianOption
{
    public GeometricAverageOption(OptionType optionType, double strikePrice, DateOnly effectiveDate, DateOnly expirationDate)
        : base(optionType, strikePrice, effectiveDate, expirationDate)
    {
    }
}
