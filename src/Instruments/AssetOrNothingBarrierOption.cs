namespace DerivaSharp.Instruments;

public sealed record AssetOrNothingBarrierOption : BinaryBarrierOption
{
    public AssetOrNothingBarrierOption(
        BarrierType barrierType,
        PaymentType rebatePaymentType,
        OptionType? optionType,
        double strikePrice,
        double barrierPrice,
        double rebate,
        int observationIntervalDays,
        DateOnly effectiveDate,
        DateOnly expirationDate)
        : base(barrierType, rebatePaymentType, optionType, strikePrice, barrierPrice, rebate, observationIntervalDays, effectiveDate, expirationDate)
    {
    }
}
