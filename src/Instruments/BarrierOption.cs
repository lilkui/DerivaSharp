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
    /// <param name="optionType">The option type (call or put).</param>
    /// <param name="barrierType">The barrier type (up-and-in, down-and-in, up-and-out, or down-and-out).</param>
    /// <param name="strikePrice">The strike price.</param>
    /// <param name="barrierPrice">The barrier price level.</param>
    /// <param name="rebate">The rebate paid if the option is not activated or is knocked out.</param>
    /// <param name="rebatePaymentType">When the rebate is paid.</param>
    /// <param name="observationIntervalDays">The interval in days between barrier observations (0 for continuous).</param>
    /// <param name="effectiveDate">The date when the option becomes effective.</param>
    /// <param name="expirationDate">The date when the option expires.</param>
    public BarrierOption(
        OptionType optionType,
        BarrierType barrierType,
        double strikePrice,
        double barrierPrice,
        double rebate,
        PaymentType rebatePaymentType,
        int observationIntervalDays,
        DateOnly effectiveDate,
        DateOnly expirationDate)
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
            : BuildObservationDates(effectiveDate, expirationDate, observationIntervalDays);
    }

    /// <summary>
    ///     Gets the barrier type (up-and-in, down-and-in, up-and-out, or down-and-out).
    /// </summary>
    public BarrierType BarrierType { get; init; }

    /// <summary>
    ///     Gets the barrier price level.
    /// </summary>
    public double BarrierPrice { get; init; }

    /// <summary>
    ///     Gets the rebate paid if the option is not activated or is knocked out.
    /// </summary>
    public double Rebate { get; init; }

    /// <summary>
    ///     Gets when the rebate is paid.
    /// </summary>
    public PaymentType RebatePaymentType { get; init; }

    /// <summary>
    ///     Gets the observation interval in years.
    /// </summary>
    public double ObservationInterval { get; init; }

    /// <summary>
    ///     Gets the dates when the barrier is observed.
    /// </summary>
    public DateOnly[] ObservationDates { get; init; }

    private static DateOnly[] BuildObservationDates(
        DateOnly effectiveDate,
        DateOnly expirationDate,
        int observationIntervalDays)
    {
        List<DateOnly> dates = [];
        DateOnly current = effectiveDate.AddDays(observationIntervalDays);
        DateOnly? lastAdded = null;

        while (current <= expirationDate)
        {
            DateOnly adjusted = AdjustToNextTradingDay(current, expirationDate);
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

    private static DateOnly AdjustToNextTradingDay(DateOnly date, DateOnly expirationDate)
    {
        DateOnly current = date;
        while (current <= expirationDate && !DateUtils.IsTradingDay(current))
        {
            current = current.AddDays(1);
        }

        return current;
    }
}
