namespace DerivaSharp.Instruments;

/// <summary>
///     Represents a binary barrier option that pays the asset price if barrier conditions are met.
/// </summary>
public sealed record AssetOrNothingBarrierOption : BinaryBarrierOption
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="AssetOrNothingBarrierOption" /> class.
    /// </summary>
    /// <param name="barrierType">One of the <see cref="BarrierType" /> enumeration values that specifies the barrier type.</param>
    /// <param name="rebatePaymentType">One of the <see cref="PaymentType" /> enumeration values that specifies when the rebate is paid.</param>
    /// <param name="optionType">One of the <see cref="OptionType" /> enumeration values that specifies the option type, or <see langword="null" /> for one-touch and no-touch options.</param>
    /// <param name="strikePrice">The strike price.</param>
    /// <param name="barrierPrice">The barrier price level.</param>
    /// <param name="rebate">The fixed amount paid.</param>
    /// <param name="observationIntervalDays">The interval in days between barrier observations.</param>
    /// <param name="effectiveDate">The date when the option becomes effective.</param>
    /// <param name="expirationDate">The date when the option expires.</param>
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
