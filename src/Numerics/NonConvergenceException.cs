namespace DerivaSharp.Numerics;

/// <summary>
///     The exception that is thrown when a numerical algorithm fails to converge.
/// </summary>
public sealed class NonConvergenceException : Exception
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="NonConvergenceException" /> class.
    /// </summary>
    public NonConvergenceException()
    {
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="NonConvergenceException" /> class with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public NonConvergenceException(string message)
        : base(message)
    {
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="NonConvergenceException" /> class with a specified
    ///     error message and a reference to the inner exception that is the cause of this exception.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public NonConvergenceException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
