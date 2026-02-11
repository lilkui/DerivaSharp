namespace DerivaSharp.Instruments;

/// <summary>
///     Represents a binary barrier option that pays the asset price if barrier conditions are met.
/// </summary>
public sealed record AssetOrNothingBarrierOption : BinaryBarrierOption
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="AssetOrNothingBarrierOption" /> class.
    /// </summary>
    /// <param name="barrierType">The barrier type.</param>
    /// <param name="rebatePaymentType">When the rebate is paid.</param>
    /// <param name="optionType">The option type (null for one-touch/no-touch options).</param>
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
