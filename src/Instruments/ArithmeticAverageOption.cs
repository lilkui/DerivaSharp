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
    /// <param name="optionType">One of the <see cref="OptionType" /> enumeration values that specifies whether the option is a call or a put.</param>
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
    /// <value>The date on which the averaging period begins.</value>
    public DateOnly AverageStartDate { get; init; }

    /// <summary>
    ///     Gets the realized average price up to the valuation date.
    /// </summary>
    /// <value>The realized average price up to the valuation date; 0 if none has been realized yet.</value>
    public double RealizedAveragePrice { get; init; }
}
