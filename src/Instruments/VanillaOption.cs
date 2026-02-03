namespace DerivaSharp.Instruments;

public abstract record VanillaOption : StrikedTypePayoffOption
{
    protected VanillaOption(OptionType optionType, Exercise exercise, double strikePrice, DateOnly effectiveDate, DateOnly expirationDate)
        : base(optionType, strikePrice, effectiveDate, expirationDate) =>
        Exercise = exercise;

    public Exercise Exercise { get; init; }
}
