using CommunityToolkit.Diagnostics;

namespace DerivaSharp.Time;

/// <summary>
///     Utility methods for trading-day calculations.
/// </summary>
public static class DateUtils
{
    /// <summary>
    ///     Determines whether <paramref name="date" /> is a trading day.
    /// </summary>
    /// <param name="date">The date to check.</param>
    /// <returns><c>true</c> if <paramref name="date" /> is a trading day; otherwise, <c>false</c>.</returns>
    public static bool IsTradingDay(DateOnly date)
    {
        return date.DayOfWeek is not (DayOfWeek.Saturday or DayOfWeek.Sunday) && !TradingCalendar.Holidays.Contains(date);
    }

    /// <summary>
    ///     Returns all trading days between <paramref name="startDate" /> and <paramref name="endDate" />, inclusive.
    /// </summary>
    /// <param name="startDate">The start date.</param>
    /// <param name="endDate">The end date.</param>
    /// <returns>An enumerable sequence of trading days.</returns>
    public static IEnumerable<DateOnly> GetTradingDays(DateOnly startDate, DateOnly endDate)
    {
        Guard.IsLessThanOrEqualTo(startDate, endDate);

        for (DateOnly date = startDate; date <= endDate; date = date.AddDays(1))
        {
            if (IsTradingDay(date))
            {
                yield return date;
            }
        }
    }

    /// <summary>
    ///     Returns monthly observation dates between <paramref name="startDate" /> and <paramref name="endDate" />,
    ///     adjusted to the next trading day when falling on a non-trading day.
    /// </summary>
    /// <param name="startDate">The start date.</param>
    /// <param name="endDate">The end date.</param>
    /// <param name="lockUpMonths">The initial lock-up period in months.</param>
    /// <returns>An enumerable sequence of observation dates.</returns>
    public static IEnumerable<DateOnly> GetObservationDates(DateOnly startDate, DateOnly endDate, int lockUpMonths)
    {
        Guard.IsLessThanOrEqualTo(startDate, endDate);
        Guard.IsGreaterThanOrEqualTo(lockUpMonths, 0);

        lockUpMonths = Math.Max(lockUpMonths, 1);
        DateOnly currentDate = startDate.AddMonths(lockUpMonths);
        while (currentDate <= endDate)
        {
            DateOnly observationDate = currentDate;
            while (observationDate <= endDate && !IsTradingDay(observationDate))
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
