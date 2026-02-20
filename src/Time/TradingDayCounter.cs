namespace DerivaSharp.Time;

public sealed class TradingDayCounter(ICalendar calendar) : IDayCounter
{
    public double YearFraction(DateOnly startDate, DateOnly endDate)
        => (double)calendar.CountTradingDays(startDate, endDate) / calendar.TradingDaysPerYear;
}
