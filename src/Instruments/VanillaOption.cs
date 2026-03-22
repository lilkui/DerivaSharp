namespace DerivaSharp.Instruments;

/// <summary>
///     Base class for vanilla options with European or American exercise.
/// </summary>
public abstract record VanillaOption : StrikedTypePayoffOption
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="VanillaOption" /> class.
    /// </summary>
    /// <param name="optionType">One of the <see cref="OptionType" /> enumeration values that specifies whether the option is a call or a put.</param>
    /// <param name="exercise">One of the <see cref="Exercise" /> enumeration values that specifies the exercise style.</param>
    /// <param name="strikePrice">The strike price.</param>
    /// <param name="effectiveDate">The date when the option becomes effective.</param>
    /// <param name="expirationDate">The date when the option expires.</param>
    protected VanillaOption(OptionType optionType, Exercise exercise, double strikePrice, DateOnly effectiveDate, DateOnly expirationDate)
        : base(optionType, strikePrice, effectiveDate, expirationDate) =>
        Exercise = exercise;

    /// <summary>
    ///     Gets the exercise style.
    /// </summary>
    /// <value>One of the <see cref="Exercise" /> values that specifies the exercise style.</value>
    public Exercise Exercise { get; init; }
}
