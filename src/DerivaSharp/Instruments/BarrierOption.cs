using CommunityToolkit.Diagnostics;

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
    }

    public BarrierType BarrierType { get; init; }

    public double BarrierPrice { get; init; }

    public double Rebate { get; init; }

    public PaymentType RebatePaymentType { get; init; }

    public double ObservationInterval { get; init; }
}
