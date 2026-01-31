using CommunityToolkit.Diagnostics;

namespace DerivaSharp.Instruments;

public sealed record ArithmeticAverageOption : AsianOption
{
    public ArithmeticAverageOption(
        OptionType optionType,
        double strikePrice,
        DateOnly averageStartDate,
        double realizedAveragePrice,
        DateOnly effectiveDate,
        DateOnly expirationDate)
        : base(optionType, strikePrice, effectiveDate, expirationDate)
    {
        Guard.IsBetweenOrEqualTo(averageStartDate, effectiveDate, expirationDate);
        Guard.IsGreaterThanOrEqualTo(realizedAveragePrice, 0);

        AverageStartDate = averageStartDate;
        RealizedAveragePrice = realizedAveragePrice;
    }

    public DateOnly AverageStartDate { get; init; }

    public double RealizedAveragePrice { get; init; }
}
