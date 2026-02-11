namespace DerivaSharp.Instruments;

/// <summary>
///     Specifies whether an option is a call or put.
/// </summary>
public enum OptionType
{
    /// <summary>
    ///     Call option giving the right to buy the underlying asset.
    /// </summary>
    Call = 1,

    /// <summary>
    ///     Put option giving the right to sell the underlying asset.
    /// </summary>
    Put = -1,
}
