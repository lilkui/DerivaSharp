namespace DerivaSharp.Time;

/// <summary>
///     Computes year fractions using actual calendar days divided by 365 (Act/365 Fixed).
/// </summary>
public sealed class CalendarDayCounter : IDayCounter
{
    /// <inheritdoc />
    public double YearFraction(DateOnly startDate, DateOnly endDate)
        => (endDate.DayNumber - startDate.DayNumber) / 365.0;
}
