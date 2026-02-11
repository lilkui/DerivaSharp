namespace DerivaSharp;

/// <summary>
///     Provides standard exception messages used throughout the library.
/// </summary>
internal static class ExceptionMessages
{
    internal const string CudaUnavailable = "CUDA is not available on this system.";
    internal const string ExplicitSchemeUnstable = "The explicit scheme is unstable with the current parameters. Reduce TimeStepCount or increase PriceStepCount.";
    internal const string InvalidBarrierType = "Invalid barrier type.";
    internal const string InvalidBinaryBarrierOption = "Invalid binary barrier option.";
    internal const string InvalidDigitalOption = "Invalid digital option.";
    internal const string InvalidFiniteDifferenceScheme = "Invalid finite difference scheme.";
    internal const string InvalidOptionType = "Invalid option type.";
    internal const string MultipleObservationsAtSameStep = "Multiple observation times map to the same time step. Increase TimeStepCount.";
    internal const string ObservationTimeNotOnGrid = "Observation time is too far from the nearest grid point. Increase TimeStepCount.";
    internal const string PayAtHitNotValidForKnockIn = "Rebate payment type 'PayAtHit' is not valid for knock-in options.";
    internal const string SpanLengthsMustMatch = "All spans must have the same length.";
}
