namespace DerivaSharp.Instruments;

public record CashOrNothingBarrierOption : BinaryBarrierOption
{
    public CashOrNothingBarrierOption(
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

    public static CashOrNothingBarrierOption CreateOneTouchUp(
        PaymentType rebatePaymentType,
        double strikePrice,
        double barrierPrice,
        double rebate,
        DateOnly effectiveDate,
        DateOnly expirationDate) =>
        new(
            BarrierType.UpAndIn,
            rebatePaymentType,
            null,
            strikePrice,
            barrierPrice,
            rebate,
            0,
            effectiveDate,
            expirationDate);

    public static CashOrNothingBarrierOption CreateOneTouchDown(
        PaymentType rebatePaymentType,
        double strikePrice,
        double barrierPrice,
        double rebate,
        DateOnly effectiveDate,
        DateOnly expirationDate) =>
        new(
            BarrierType.DownAndIn,
            rebatePaymentType,
            null,
            strikePrice,
            barrierPrice,
            rebate,
            0,
            effectiveDate,
            expirationDate);

    public static CashOrNothingBarrierOption CreateNoTouchUp(
        PaymentType rebatePaymentType,
        double strikePrice,
        double barrierPrice,
        double rebate,
        DateOnly effectiveDate,
        DateOnly expirationDate) =>
        new(
            BarrierType.UpAndOut,
            rebatePaymentType,
            null,
            strikePrice,
            barrierPrice,
            rebate,
            0,
            effectiveDate,
            expirationDate);

    public static CashOrNothingBarrierOption CreateNoTouchDown(
        PaymentType rebatePaymentType,
        double strikePrice,
        double barrierPrice,
        double rebate,
        DateOnly effectiveDate,
        DateOnly expirationDate) =>
        new(
            BarrierType.DownAndOut,
            rebatePaymentType,
            null,
            strikePrice,
            barrierPrice,
            rebate,
            0,
            effectiveDate,
            expirationDate);
}
