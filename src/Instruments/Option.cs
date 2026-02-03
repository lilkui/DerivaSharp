using CommunityToolkit.Diagnostics;

namespace DerivaSharp.Instruments;

public abstract record Option
{
    protected Option(DateOnly effectiveDate, DateOnly expirationDate)
    {
        Guard.IsGreaterThanOrEqualTo(expirationDate, effectiveDate);

        EffectiveDate = effectiveDate;
        ExpirationDate = expirationDate;
    }

    public DateOnly EffectiveDate { get; init; }

    public DateOnly ExpirationDate { get; init; }
}
