using CommunityToolkit.Diagnostics;

namespace DerivaSharp.Time;

/// <summary>
///     Provides year-fraction calculations under different day-count conventions.
/// </summary>
public static class DayCounter
{
    /// <summary>
    ///     Returns the year fraction between two dates using the Actual/365 Fixed convention.
    /// </summary>
    /// <param name="startDate">The start date (must be on or before <paramref name="endDate" />).</param>
    /// <param name="endDate">The end date.</param>
    /// <returns>The year fraction as the number of actual calendar days divided by 365.</returns>
    public static double YearFraction(DateOnly startDate, DateOnly endDate)
    {
        Guard.IsLessThanOrEqualTo(startDate, endDate);
        return (endDate.DayNumber - startDate.DayNumber) / 365.0;
    }

    /// <summary>
    ///     Returns the year fraction between two dates using trading-day count from a given calendar.
    /// </summary>
    /// <param name="startDate">The start date (must be on or before <paramref name="endDate" />).</param>
    /// <param name="endDate">The end date.</param>
    /// <param name="calendar">The trading calendar used to count business days and determine days per year.</param>
    /// <returns>The year fraction as the number of trading days between the two dates divided by <see cref="ICalendar.TradingDaysPerYear" />.</returns>
    public static double YearFraction(DateOnly startDate, DateOnly endDate, ICalendar calendar)
    {
        Guard.IsLessThanOrEqualTo(startDate, endDate);
        Guard.IsNotNull(calendar);

        return (double)calendar.CountTradingDays(startDate, endDate) / calendar.TradingDaysPerYear;
    }
}
