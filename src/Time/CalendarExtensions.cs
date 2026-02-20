using CommunityToolkit.Diagnostics;

namespace DerivaSharp.Time;

/// <summary>
///     Extension methods for <see cref="ICalendar" /> that enumerate and count trading days.
/// </summary>
public static class CalendarExtensions
{
    extension(ICalendar calendar)
    {
        /// <summary>
        ///     Returns all trading days between <paramref name="startDate" /> and <paramref name="endDate" />, inclusive.
        /// </summary>
        /// <param name="startDate">The start date.</param>
        /// <param name="endDate">The end date.</param>
        /// <returns>An enumerable sequence of trading days.</returns>
        public IEnumerable<DateOnly> GetTradingDays(DateOnly startDate, DateOnly endDate)
        {
            Guard.IsLessThanOrEqualTo(startDate, endDate);

            for (DateOnly date = startDate; date <= endDate; date = date.AddDays(1))
            {
                if (calendar.IsTradingDay(date))
                {
                    yield return date;
                }
            }
        }

        /// <summary>
        ///     Returns the number of trading days from <paramref name="startDate" /> up to, but not including,
        ///     <paramref name="endDate" />.
        /// </summary>
        /// <param name="startDate">The start date.</param>
        /// <param name="endDate">The end date.</param>
        /// <returns>The count of trading days in the half-open interval [startDate, endDate).</returns>
        public int CountTradingDays(DateOnly startDate, DateOnly endDate)
        {
            Guard.IsLessThanOrEqualTo(startDate, endDate);

            int count = 0;
            for (DateOnly date = startDate; date < endDate; date = date.AddDays(1))
            {
                if (calendar.IsTradingDay(date))
                {
                    count++;
                }
            }

            return count;
        }

        /// <summary>
        ///     Returns monthly observation dates between <paramref name="startDate" /> and <paramref name="endDate" />,
        ///     each adjusted forward to the next trading day when falling on a non-trading day.
        /// </summary>
        /// <param name="startDate">The schedule start date.</param>
        /// <param name="endDate">The schedule end date.</param>
        /// <param name="lockUpMonths">The initial lock-up period in whole months before the first observation date.</param>
        /// <returns>An enumerable sequence of adjusted observation dates.</returns>
        public IEnumerable<DateOnly> GetMonthlyObservationDates(DateOnly startDate, DateOnly endDate, int lockUpMonths)
        {
            Guard.IsLessThanOrEqualTo(startDate, endDate);
            Guard.IsGreaterThanOrEqualTo(lockUpMonths, 0);

            lockUpMonths = Math.Max(lockUpMonths, 1);
            DateOnly currentDate = startDate.AddMonths(lockUpMonths);
            while (currentDate <= endDate)
            {
                DateOnly observationDate = currentDate;
                while (observationDate <= endDate && !calendar.IsTradingDay(observationDate))
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
}
