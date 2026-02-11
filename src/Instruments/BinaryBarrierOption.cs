using CommunityToolkit.Diagnostics;

namespace DerivaSharp.Instruments;

/// <summary>
///     Base class for binary barrier options that pay a fixed amount based on barrier conditions.
/// </summary>
public abstract record BinaryBarrierOption : Option
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="BinaryBarrierOption" /> class.
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

    /// <summary>
    ///     Gets the barrier type.
    /// </summary>
    public BarrierType BarrierType { get; init; }

    /// <summary>
    ///     Gets the option type (null for one-touch/no-touch options).
    /// </summary>
    public OptionType? OptionType { get; init; }

    /// <summary>
    ///     Gets when the rebate is paid.
    /// </summary>
    public PaymentType RebatePaymentType { get; init; }

    /// <summary>
    ///     Gets the strike price.
    /// </summary>
    public double StrikePrice { get; init; }

    /// <summary>
    ///     Gets the barrier price level.
    /// </summary>
    public double BarrierPrice { get; init; }

    /// <summary>
    ///     Gets the fixed amount paid.
    /// </summary>
    public double Rebate { get; init; }

    /// <summary>
    ///     Gets the observation interval in years.
    /// </summary>
    public double ObservationInterval { get; init; }
}
