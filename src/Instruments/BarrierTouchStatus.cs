namespace DerivaSharp.Instruments;

/// <summary>
///     Indicates whether a barrier has been touched and from which direction.
/// </summary>
public enum BarrierTouchStatus
{
    /// <summary>
    ///     Barrier has not been touched.
    /// </summary>
    NoTouch,

    /// <summary>
    ///     Barrier was touched from below (price moved up through barrier).
    /// </summary>
    UpTouch,

    /// <summary>
    ///     Barrier was touched from above (price moved down through barrier).
    /// </summary>
    DownTouch,
}
