namespace DerivaSharp;

internal static class ExceptionMessages
{
    internal const string CudaUnavailable = "CUDA is not available on this system.";
    internal const string ExplicitSchemeUnstable = "The explicit scheme is unstable with the current parameters. Reduce the time step or increase the price step.";
    internal const string InvalidBarrierType = "Invalid barrier type.";
    internal const string InvalidBinaryBarrierOption = "Invalid binary barrier option.";
    internal const string InvalidDigitalOption = "Invalid digital option.";
    internal const string InvalidFiniteDifferenceScheme = "Invalid finite difference scheme.";
    internal const string InvalidOptionType = "Invalid option type.";
    internal const string InvalidTouchType = "Invalid touch type.";
    internal const string PayAtHitNotValidForKnockIn = "Rebate payment type 'PayAtHit' is not valid for knock-in options.";
    internal const string SpanLengthsMustMatch = "All spans must have the same length.";
    internal const string UseCashParityForNoTouch = "Use cash parity for no-touch options.";
}
