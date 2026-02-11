namespace DerivaSharp.Instruments;

/// <summary>
///     Specifies the exercise style of an option.
/// </summary>
public enum Exercise
{
    /// <summary>
    ///     Option can only be exercised at expiration.
    /// </summary>
    European,

    /// <summary>
    ///     Option can be exercised at any time before expiration.
    /// </summary>
    American,
}
