namespace NexMediator.Abstractions.Context;

/// <summary>
/// Provides access to the current Correlation ID.
/// </summary>
/// <remarks>
/// A Correlation ID is used to track a request across systems.
/// It helps group logs and debug problems.
/// </remarks>
public interface ICorrelationContext
{
    /// <summary>
    /// Gets the correlation ID for the current request.
    /// </summary>
    string CorrelationId { get; }

    /// <summary>
    /// Sets the correlation ID if not already set.
    /// </summary>
    /// <param name="id">The correlation ID to assign.</param>
    void SetCorrelationId(string id);
}
