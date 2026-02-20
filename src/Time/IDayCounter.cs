namespace DerivaSharp.Time;

/// <summary>
///     Converts a date interval into a year fraction used for discounting and accrual.
/// </summary>
public interface IDayCounter
{
    /// <summary>
    ///     Returns the year fraction between <paramref name="startDate" /> and <paramref name="endDate" />.
    /// </summary>
    /// <param name="startDate">The start date of the period.</param>
    /// <param name="endDate">The end date of the period.</param>
    /// <returns>The year fraction as a non-negative <see cref="double" />.</returns>
    double YearFraction(DateOnly startDate, DateOnly endDate);
}
