using CommunityToolkit.Diagnostics;

namespace DerivaSharp.Instruments;

/// <summary>
///     Represents an Asian option with payoff based on the arithmetic average of the underlying asset price.
/// </summary>
public sealed record ArithmeticAverageOption : AsianOption
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="ArithmeticAverageOption" /> class.
    /// </summary>
    /// <param name="optionType">The option type (call or put).</param>
    /// <param name="strikePrice">The strike price.</param>
    /// <param name="averageStartDate">The date when averaging begins.</param>
    /// <param name="realizedAveragePrice">The realized average price up to the valuation date.</param>
    /// <param name="effectiveDate">The date when the option becomes effective.</param>
    /// <param name="expirationDate">The date when the option expires.</param>
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

    /// <summary>
    ///     Gets the date when averaging begins.
    /// </summary>
    public DateOnly AverageStartDate { get; init; }

    /// <summary>
    ///     Gets the realized average price up to the valuation date.
    /// </summary>
    public double RealizedAveragePrice { get; init; }
}
