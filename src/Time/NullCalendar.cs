namespace DerivaSharp.Time;

/// <summary>
///     A calendar in which every day is a trading day.
/// </summary>
public sealed class NullCalendar : ICalendar
{
    /// <inheritdoc />
    public int TradingDaysPerYear => 365;

    /// <summary>
    ///     Always returns <see langword="true" />; all dates are considered trading days.
    /// </summary>
    public bool IsTradingDay(DateOnly date) => true;
}
