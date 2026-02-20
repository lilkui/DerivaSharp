namespace DerivaSharp.Time;

public interface IDayCounter
{
    double YearFraction(DateOnly startDate, DateOnly endDate);
}
