namespace DerivaSharp.Instruments;

/// <summary>
///     Specifies the type of barrier for barrier options.
/// </summary>
public enum BarrierType
{
    /// <summary>
    ///     Option activates when the underlying price moves up through the barrier.
    /// </summary>
    UpAndIn,

    /// <summary>
    ///     Option activates when the underlying price moves down through the barrier.
    /// </summary>
    DownAndIn,

    /// <summary>
    ///     Option deactivates when the underlying price moves up through the barrier.
    /// </summary>
    UpAndOut,

    /// <summary>
    ///     Option deactivates when the underlying price moves down through the barrier.
    /// </summary>
    DownAndOut,
}
