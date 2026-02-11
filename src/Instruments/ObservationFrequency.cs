namespace DerivaSharp.Instruments;

/// <summary>
///     Specifies how frequently barrier levels are observed.
/// </summary>
public enum ObservationFrequency
{
    /// <summary>
    ///     Barrier is observed daily.
    /// </summary>
    Daily,

    /// <summary>
    ///     Barrier is observed only at expiration.
    /// </summary>
    AtExpiry,
}
