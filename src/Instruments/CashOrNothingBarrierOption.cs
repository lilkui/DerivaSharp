namespace DerivaSharp.Instruments;

/// <summary>
///     Represents a binary barrier option that pays a fixed cash amount if barrier conditions are met.
/// </summary>
public record CashOrNothingBarrierOption : BinaryBarrierOption
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="CashOrNothingBarrierOption" /> class.
    /// </summary>
    /// <param name="barrierType">One of the <see cref="BarrierType" /> enumeration values that specifies the barrier type.</param>
    /// <param name="rebatePaymentType">One of the <see cref="PaymentType" /> enumeration values that specifies when the rebate is paid.</param>
    /// <param name="optionType">One of the <see cref="OptionType" /> enumeration values that specifies the option type, or <see langword="null" /> for one-touch and no-touch options.</param>
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
    /// <param name="rebatePaymentType">One of the <see cref="PaymentType" /> enumeration values that specifies when the rebate is paid.</param>
    /// <param name="strikePrice">The strike price of the option.</param>
    /// <param name="barrierPrice">The upper barrier price level.</param>
    /// <param name="rebate">The fixed cash amount paid when the barrier is touched.</param>
    /// <param name="effectiveDate">The date when the option becomes effective.</param>
    /// <param name="expirationDate">The date when the option expires.</param>
    /// <returns>A new <see cref="CashOrNothingBarrierOption" /> configured as an up one-touch.</returns>
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
    /// <param name="rebatePaymentType">One of the <see cref="PaymentType" /> enumeration values that specifies when the rebate is paid.</param>
    /// <param name="strikePrice">The strike price of the option.</param>
    /// <param name="barrierPrice">The lower barrier price level.</param>
    /// <param name="rebate">The fixed cash amount paid when the barrier is touched.</param>
    /// <param name="effectiveDate">The date when the option becomes effective.</param>
    /// <param name="expirationDate">The date when the option expires.</param>
    /// <returns>A new <see cref="CashOrNothingBarrierOption" /> configured as a down one-touch.</returns>
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
    /// <param name="rebatePaymentType">One of the <see cref="PaymentType" /> enumeration values that specifies when the rebate is paid.</param>
    /// <param name="strikePrice">The strike price of the option.</param>
    /// <param name="barrierPrice">The upper barrier price level.</param>
    /// <param name="rebate">The fixed cash amount paid at expiry if the barrier is not touched.</param>
    /// <param name="effectiveDate">The date when the option becomes effective.</param>
    /// <param name="expirationDate">The date when the option expires.</param>
    /// <returns>A new <see cref="CashOrNothingBarrierOption" /> configured as an up no-touch.</returns>
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
    /// <param name="rebatePaymentType">One of the <see cref="PaymentType" /> enumeration values that specifies when the rebate is paid.</param>
    /// <param name="strikePrice">The strike price of the option.</param>
    /// <param name="barrierPrice">The lower barrier price level.</param>
    /// <param name="rebate">The fixed cash amount paid at expiry if the barrier is not touched.</param>
    /// <param name="effectiveDate">The date when the option becomes effective.</param>
    /// <param name="expirationDate">The date when the option expires.</param>
    /// <returns>A new <see cref="CashOrNothingBarrierOption" /> configured as a down no-touch.</returns>
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
