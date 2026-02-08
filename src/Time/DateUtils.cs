using CommunityToolkit.Diagnostics;

namespace DerivaSharp.Time;

public static class DateUtils
{
    /// <summary>
    ///     Determines whether the specified date is a trading day.
    /// </summary>
    /// <param name="date">The date to evaluate.</param>
    /// <returns><c>true</c> if the date is a trading day; otherwise, <c>false</c>.</returns>
    public static bool IsTradingDay(DateOnly date)
    {
        return date.DayOfWeek is not (DayOfWeek.Saturday or DayOfWeek.Sunday) && !TradingCalendar.Holidays.Contains(date);
    }

    /// <summary>
    ///     Gets all trading days between a specified start and end date, inclusive.
    /// </summary>
    /// <param name="startDate">The start date of the period.</param>
    /// <param name="endDate">The end date of the period.</param>
    /// <returns>An enumerable sequence of trading days.</returns>
    public static IEnumerable<DateOnly> GetTradingDays(DateOnly startDate, DateOnly endDate)
    {
        Guard.IsLessThanOrEqualTo(startDate, endDate);

        for (DateOnly date = startDate; date <= endDate; date = date.AddDays(1))
        {
            if (date.DayOfWeek != DayOfWeek.Saturday && date.DayOfWeek != DayOfWeek.Sunday && !TradingCalendar.Holidays.Contains(date))
            {
                yield return date;
            }
        }
    }

    /// <summary>
    ///     Generates a sequence of monthly observation dates, adjusted to the next business day if they fall on a non-trading
    ///     day.
    /// </summary>
    /// <param name="startDate">The start date from which to calculate observation dates.</param>
    /// <param name="endDate">The end date of the period. No observation dates will be generated after this date.</param>
    /// <param name="lockUpMonths">
    ///     The initial lock-up period in months. The first observation date will be at least this many
    ///     months after the start date. If 0, it defaults to 1.
    /// </param>
    /// <returns>An enumerable sequence of observation dates.</returns>
    public static IEnumerable<DateOnly> GetObservationDates(DateOnly startDate, DateOnly endDate, int lockUpMonths)
    {
        Guard.IsLessThanOrEqualTo(startDate, endDate);
        Guard.IsGreaterThanOrEqualTo(lockUpMonths, 0);

        if (lockUpMonths == 0)
        {
            lockUpMonths = 1;
        }

        DateOnly currentDate = startDate.AddMonths(lockUpMonths);
        while (currentDate <= endDate)
        {
            DateOnly observationDate = currentDate;
            while (observationDate <= endDate &&
                   (observationDate.DayOfWeek == DayOfWeek.Saturday ||
                    observationDate.DayOfWeek == DayOfWeek.Sunday ||
                    TradingCalendar.Holidays.Contains(observationDate)))
            {
                observationDate = observationDate.AddDays(1);
            }

            if (observationDate <= endDate)
            {
                yield return observationDate;
            }

            currentDate = currentDate.AddMonths(1);
        }
    }
}
