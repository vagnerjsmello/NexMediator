namespace NexMediator.Abstractions.Interfaces;

/// <summary>
/// Processes responses after handler execution
/// </summary>
/// <typeparam name="TRequest">Request type</typeparam>
/// <typeparam name="TResponse">Response type</typeparam>
/// <remarks>
/// Common uses:
/// - Response enrichment
/// - Metrics collection
/// - Audit logging
/// - Cache updates
/// - Notifications
/// </remarks>
public interface INexRequestPostProcessor<in TRequest, in TResponse>
{
    /// <summary>
    /// Post-processes a response
    /// </summary>
    /// <param name="request">Original request</param>
    /// <param name="response">Handler response</param>
    /// <param name="cancellationToken">Optional cancellation token</param>
    /// <returns>Task representing completion</returns>
    /// <remarks>
    /// Guidelines:
    /// - Avoid exceptions
    /// - Keep processing light
    /// - Respect immutability
    /// - Check cancellation
    /// </remarks>
    Task Process(TRequest request, TResponse response, CancellationToken cancellationToken);
}
