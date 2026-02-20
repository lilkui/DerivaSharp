namespace DerivaSharp.Time;

public sealed class CalendarDayCounter : IDayCounter
{
    public double YearFraction(DateOnly startDate, DateOnly endDate)
        => (endDate.DayNumber - startDate.DayNumber) / 365.0;
}
