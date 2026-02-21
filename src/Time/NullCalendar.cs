namespace DerivaSharp.Time;

/// <summary>
///     A calendar in which every day is a trading day.
/// </summary>
public sealed class NullCalendar : ICalendar
{
    private NullCalendar()
    {
    }

    /// <summary>
    ///     Gets the shared singleton instance of <see cref="NullCalendar" />.
    /// </summary>
    public static NullCalendar Shared { get; } = new();

    /// <inheritdoc />
    public int TradingDaysPerYear => 365;

    /// <summary>
    ///     Always returns <see langword="true" />; all dates are considered trading days.
    /// </summary>
    public bool IsTradingDay(DateOnly date) => true;
}
