namespace NexMediator.Abstractions.Context;

/// <summary>
/// Default implementation of ICorrelationContext using AsyncLocal.
/// </summary>
/// <remarks>
/// Stores a unique Correlation ID per async flow (like a request).
/// This helps to track and group logs from the same operation.
/// </remarks>
public class CorrelationContext : ICorrelationContext
{
    private static readonly AsyncLocal<string?> _current = new();

    /// <summary>
    /// Gets the current Correlation ID. Throws if not yet set.
    /// </summary>
    public string CorrelationId =>
        _current.Value ?? throw new InvalidOperationException("Correlation ID has not been set.");

    /// <summary>
    /// Sets the Correlation ID only if not already defined.
    /// </summary>
    public void SetCorrelationId(string id)
    {
        if (_current.Value is null)
            _current.Value = id;
    }
}
