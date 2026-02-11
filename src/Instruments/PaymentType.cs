namespace DerivaSharp.Instruments;

/// <summary>
///     Specifies when a rebate or payment is made.
/// </summary>
public enum PaymentType
{
    /// <summary>
    ///     Payment is made immediately when the barrier is hit.
    /// </summary>
    PayAtHit,

    /// <summary>
    ///     Payment is made at option expiration.
    /// </summary>
    PayAtExpiry,
}
