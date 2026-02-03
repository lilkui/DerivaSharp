using CommunityToolkit.Diagnostics;

namespace DerivaSharp.Instruments;

public abstract record StrikedTypePayoffOption : Option
{
    protected StrikedTypePayoffOption(OptionType optionType, double strikePrice, DateOnly effectiveDate, DateOnly expirationDate)
        : base(effectiveDate, expirationDate)
    {
        Guard.IsGreaterThan(strikePrice, 0);

        OptionType = optionType;
        StrikePrice = strikePrice;
    }

    public OptionType OptionType { get; init; }

    public double StrikePrice { get; init; }
}
