namespace DerivaSharp.Time;

public interface ICalendar
{
    int TradingDaysPerYear { get; }

    bool IsTradingDay(DateOnly date);
}
