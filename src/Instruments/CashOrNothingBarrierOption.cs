namespace DerivaSharp.Instruments;

/// <summary>
///     Represents a binary barrier option that pays a fixed cash amount if barrier conditions are met.
/// </summary>
public record CashOrNothingBarrierOption : BinaryBarrierOption
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="CashOrNothingBarrierOption" /> class.
    /// </summary>
    /// <param name="barrierType">The barrier type.</param>
    /// <param name="rebatePaymentType">When the rebate is paid.</param>
    /// <param name="optionType">The option type (null for one-touch/no-touch options).</param>
    /// <param name="strikePrice">The strike price.</param>
    /// <param name="barrierPrice">The barrier price level.</param>
    /// <param name="rebate">The fixed cash amount paid.</param>
    /// <param name="observationIntervalDays">The interval in days between barrier observations.</param>
    /// <param name="effectiveDate">The date when the option becomes effective.</param>
    /// <param name="expirationDate">The date when the option expires.</param>
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

    /// <summary>
    ///     Creates an up one-touch option that pays if the barrier is touched from below.
    /// </summary>
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

    /// <summary>
    ///     Creates a down one-touch option that pays if the barrier is touched from above.
    /// </summary>
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

    /// <summary>
    ///     Creates an up no-touch option that pays if the upper barrier is not touched.
    /// </summary>
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

    /// <summary>
    ///     Creates a down no-touch option that pays if the lower barrier is not touched.
    /// </summary>
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
