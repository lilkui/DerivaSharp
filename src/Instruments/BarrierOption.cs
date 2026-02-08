using CommunityToolkit.Diagnostics;
using DerivaSharp.Time;

namespace DerivaSharp.Instruments;

public sealed record BarrierOption : StrikedTypePayoffOption
{
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

    public BarrierType BarrierType { get; init; }

    public double BarrierPrice { get; init; }

    public double Rebate { get; init; }

    public PaymentType RebatePaymentType { get; init; }

    public double ObservationInterval { get; init; }

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
