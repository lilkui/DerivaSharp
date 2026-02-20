namespace DerivaSharp.Time;

/// <summary>
///     Computes year fractions by dividing the number of trading days in the period
///     by the calendar's conventional trading days per year.
/// </summary>
/// <param name="calendar">The trading calendar used to count trading days.</param>
public sealed class TradingDayCounter(ICalendar calendar) : IDayCounter
{
    /// <inheritdoc />
    public double YearFraction(DateOnly startDate, DateOnly endDate)
        => (double)calendar.CountTradingDays(startDate, endDate) / calendar.TradingDaysPerYear;
}
