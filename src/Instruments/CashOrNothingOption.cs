namespace DerivaSharp.Instruments;

public sealed record CashOrNothingOption : DigitalOption
{
    public CashOrNothingOption(OptionType optionType, double strikePrice, double rebate, DateOnly effectiveDate, DateOnly expirationDate)
        : base(optionType, strikePrice, effectiveDate, expirationDate) =>
        Rebate = rebate;

    public double Rebate { get; init; }
}
