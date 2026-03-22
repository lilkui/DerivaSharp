using CommunityToolkit.Diagnostics;
using DerivaSharp.Time;

namespace DerivaSharp.Instruments;

/// <summary>
///     Represents a barrier option that activates or deactivates when the underlying price crosses a barrier level.
/// </summary>
public sealed record BarrierOption : StrikedTypePayoffOption
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="BarrierOption" /> class.
    /// </summary>
    /// <param name="optionType">One of the <see cref="OptionType" /> enumeration values that specifies whether the option is a call or a put.</param>
    /// <param name="barrierType">One of the <see cref="BarrierType" /> enumeration values that specifies the barrier type.</param>
    /// <param name="strikePrice">The strike price.</param>
    /// <param name="barrierPrice">The barrier price level.</param>
    /// <param name="rebate">The rebate paid if the option is not activated or is knocked out.</param>
    /// <param name="rebatePaymentType">One of the <see cref="PaymentType" /> enumeration values that specifies when the rebate is paid.</param>
    /// <param name="observationIntervalDays">The interval in days between barrier observations (0 for continuous).</param>
    /// <param name="effectiveDate">The date when the option becomes effective.</param>
    /// <param name="expirationDate">The date when the option expires.</param>
    /// <param name="calendar">The trading calendar used for adjusting observation dates.</param>
    public BarrierOption(
        OptionType optionType,
        BarrierType barrierType,
        double strikePrice,
        double barrierPrice,
        double rebate,
        PaymentType rebatePaymentType,
        int observationIntervalDays,
        DateOnly effectiveDate,
        DateOnly expirationDate,
        ICalendar calendar)
        : base(optionType, strikePrice, effectiveDate, expirationDate)
    {
        Guard.IsGreaterThan(barrierPrice, 0);
        Guard.IsGreaterThanOrEqualTo(observationIntervalDays, 0);

        if (barrierType is BarrierType.UpAndIn or BarrierType.DownAndIn && rebatePaymentType is PaymentType.PayAtHit)
        {
            ThrowHelper.ThrowArgumentException(ExceptionMessages.PayAtHitNotValidForKnockIn);
        }

        BarrierType = barrierType;
        BarrierPrice = barrierPrice;
        Rebate = rebate;
        RebatePaymentType = rebatePaymentType;
        ObservationInterval = observationIntervalDays / 365.0;
        ObservationDates = observationIntervalDays == 0
            ? []
            : BuildObservationDates(effectiveDate, expirationDate, observationIntervalDays, calendar);
    }

    /// <summary>
    ///     Gets the barrier type (up-and-in, down-and-in, up-and-out, or down-and-out).
    /// </summary>
    /// <value>One of the <see cref="BarrierType" /> values that specifies the barrier type.</value>
    public BarrierType BarrierType { get; init; }

    /// <summary>
    ///     Gets the barrier price level.
    /// </summary>
    /// <value>The barrier price level that triggers a knock-in or knock-out event.</value>
    public double BarrierPrice { get; init; }

    /// <summary>
    ///     Gets the rebate paid if the option is not activated or is knocked out.
    /// </summary>
    /// <value>The fixed rebate amount paid if the option is not activated or is knocked out.</value>
    public double Rebate { get; init; }

    /// <summary>
    ///     Gets the payment type that specifies when the rebate is paid.
    /// </summary>
    /// <value>One of the <see cref="PaymentType" /> values that specifies when the rebate is paid.</value>
    public PaymentType RebatePaymentType { get; init; }

    /// <summary>
    ///     Gets the observation interval in years.
    /// </summary>
    /// <value>The observation interval in years; 0 if the barrier is observed continuously.</value>
    public double ObservationInterval { get; init; }

    /// <summary>
    ///     Gets the dates when the barrier is observed.
    /// </summary>
    /// <value>A read-only list of dates on which the barrier is checked; empty if the barrier is observed continuously.</value>
    public IReadOnlyList<DateOnly> ObservationDates { get; init; }

    private static IReadOnlyList<DateOnly> BuildObservationDates(
        DateOnly effectiveDate,
        DateOnly expirationDate,
        int observationIntervalDays,
        ICalendar calendar)
    {
        List<DateOnly> dates = [];
        DateOnly current = effectiveDate.AddDays(observationIntervalDays);
        DateOnly? lastAdded = null;

        while (current <= expirationDate)
        {
            DateOnly adjusted = AdjustToNextTradingDay(current, expirationDate, calendar);
            if (adjusted > expirationDate)
            {
                break;
            }

            if (lastAdded is null || adjusted != lastAdded.Value)
            {
                dates.Add(adjusted);
                lastAdded = adjusted;
            }

            current = current.AddDays(observationIntervalDays);
        }

        if (dates.Count == 0 || dates[^1] != expirationDate)
        {
            if (lastAdded is null || expirationDate != lastAdded.Value)
            {
                dates.Add(expirationDate);
            }
        }

        return dates.ToArray();
    }

    private static DateOnly AdjustToNextTradingDay(DateOnly date, DateOnly expirationDate, ICalendar calendar)
    {
        DateOnly current = date;
        while (current <= expirationDate && !calendar.IsTradingDay(current))
        {
            current = current.AddDays(1);
        }

        return current;
    }
}
