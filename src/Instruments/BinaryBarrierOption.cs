using CommunityToolkit.Diagnostics;

namespace DerivaSharp.Instruments;

public abstract record BinaryBarrierOption : Option
{
    protected BinaryBarrierOption(
        BarrierType barrierType,
        PaymentType rebatePaymentType,
        OptionType? optionType,
        double strikePrice,
        double barrierPrice,
        double rebate,
        int observationIntervalDays,
        DateOnly effectiveDate,
        DateOnly expirationDate)
        : base(effectiveDate, expirationDate)
    {
        Guard.IsGreaterThan(strikePrice, 0);
        Guard.IsGreaterThan(barrierPrice, 0);
        Guard.IsGreaterThanOrEqualTo(observationIntervalDays, 0);

        BarrierType = barrierType;
        RebatePaymentType = rebatePaymentType;
        OptionType = optionType;
        StrikePrice = strikePrice;
        BarrierPrice = barrierPrice;
        Rebate = rebate;
        ObservationInterval = observationIntervalDays / 365.0;
    }

    public BarrierType BarrierType { get; init; }

    public OptionType? OptionType { get; init; }

    public PaymentType RebatePaymentType { get; init; }

    public double StrikePrice { get; init; }

    public double BarrierPrice { get; init; }

    public double Rebate { get; init; }

    public double ObservationInterval { get; init; }
}
