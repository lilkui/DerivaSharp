namespace DerivaSharp.Time;

/// <summary>
///     Defines a trading calendar that identifies business days and annual trading-day counts.
/// </summary>
public interface ICalendar
{
    /// <summary>
    ///     Gets the conventional number of trading days in a year for this exchange.
    /// </summary>
    int TradingDaysPerYear { get; }

    /// <summary>
    ///     Determines whether <paramref name="date" /> is a trading day on this exchange.
    /// </summary>
    /// <param name="date">The date to test.</param>
    /// <returns>
    ///     <see langword="true" /> if <paramref name="date" /> is a trading day;
    ///     <see langword="false" /> if it falls on a weekend or a published exchange holiday.
    /// </returns>
    bool IsTradingDay(DateOnly date);
}
