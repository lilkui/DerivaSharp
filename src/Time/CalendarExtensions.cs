using CommunityToolkit.Diagnostics;

namespace DerivaSharp.Time;

public static class CalendarExtensions
{
    extension(ICalendar calendar)
    {
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
