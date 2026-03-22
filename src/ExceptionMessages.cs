namespace DerivaSharp;

/// <summary>
///     Provides standard exception messages used throughout the library.
/// </summary>
internal static class ExceptionMessages
{
    /// <summary>CUDA is not available on this system.</summary>
    internal const string CudaUnavailable = "CUDA is not available on this system.";

    /// <summary>The explicit scheme is unstable with the current parameters.</summary>
    internal const string ExplicitSchemeUnstable = "The explicit scheme is unstable with the current parameters. Reduce TimeStepCount or increase PriceStepCount.";

    /// <summary>Invalid barrier type.</summary>
    internal const string InvalidBarrierType = "Invalid barrier type.";

    /// <summary>Invalid binary barrier option.</summary>
    internal const string InvalidBinaryBarrierOption = "Invalid binary barrier option.";

    /// <summary>Invalid digital option.</summary>
    internal const string InvalidDigitalOption = "Invalid digital option.";

    /// <summary>Invalid finite difference scheme.</summary>
    internal const string InvalidFiniteDifferenceScheme = "Invalid finite difference scheme.";

    /// <summary>Invalid option type.</summary>
    internal const string InvalidOptionType = "Invalid option type.";

    /// <summary>Multiple observation times map to the same time step.</summary>
    internal const string MultipleObservationsAtSameStep = "Multiple observation times map to the same time step. Increase TimeStepCount.";

    /// <summary>Observation time is too far from the nearest grid point.</summary>
    internal const string ObservationTimeNotOnGrid = "Observation time is too far from the nearest grid point. Increase TimeStepCount.";

    /// <summary>Rebate payment type 'PayAtHit' is not valid for knock-in options.</summary>
    internal const string PayAtHitNotValidForKnockIn = "Rebate payment type 'PayAtHit' is not valid for knock-in options.";

    /// <summary>All spans must have the same length.</summary>
    internal const string SpanLengthsMustMatch = "All spans must have the same length.";
}
