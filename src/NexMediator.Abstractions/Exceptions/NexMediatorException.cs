namespace NexMediator.Abstractions.Exceptions;

/// <summary>
/// Represents errors that occur during mediator operations
/// </summary>
/// <remarks>
/// This exception is thrown when there are issues with mediator operations such as
/// missing handlers, invalid pipeline configurations, or other mediator-specific errors.
/// </remarks>
public class NexMediatorException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NexMediatorException"/> class with a specified error message
    /// </summary>
    /// <param name="message">The message that describes the error</param>
    public NexMediatorException(string message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="NexMediatorException"/> class with a specified error message
    /// and a reference to the inner exception that is the cause of this exception
    /// </summary>
    /// <param name="message">The message that describes the error</param>
    /// <param name="innerException">The exception that is the cause of the current exception</param>
    public NexMediatorException(string message, Exception innerException) : base(message, innerException)
    {
    }
}