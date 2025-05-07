namespace NexMediator.Abstractions.Interfaces;

/// <summary>
/// Core mediator interface for request/response and notification handling
/// </summary>
/// <remarks>
/// Supports three patterns:
/// - Request/Response: One handler per request
/// - Notifications: Multiple handlers possible
/// - Streaming: Async stream of responses
/// 
/// Core responsibilities:
/// - Route requests to handlers
/// - Execute pipeline behaviors
/// - Manage notifications
/// - Support cancellation
/// </remarks>
public interface INexMediator
{
    /// <summary>
    /// Sends a request to its handler
    /// </summary>
    /// <typeparam name="TResponse">Response type</typeparam>
    /// <param name="request">Request to process</param>
    /// <param name="cancellationToken">Optional cancellation token</param>
    /// <returns>Handler response</returns>
    /// <remarks>
    /// Processing steps:
    /// - Locate handler
    /// - Execute behaviors
    /// - Return response
    /// </remarks>
    Task<TResponse> Send<TResponse>(INexRequest<TResponse> request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Publishes a notification to all handlers
    /// </summary>
    /// <typeparam name="TNotification">Notification type</typeparam>
    /// <param name="notification">Notification to publish</param>
    /// <param name="cancellationToken">Optional cancellation token</param>
    /// <remarks>
    /// Characteristics:
    /// - Parallel execution
    /// - Independent handlers
    /// - Continues on failures
    /// </remarks>
    Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default) where TNotification : INexNotification;

    /// <summary>
    /// Processes a streaming request
    /// </summary>
    /// <typeparam name="TResponse">Response item type</typeparam>
    /// <param name="request">Stream request</param>
    /// <param name="cancellationToken">Optional cancellation token</param>
    /// <returns>Stream of responses</returns>
    /// <remarks>
    /// Features:
    /// - Async streaming
    /// - Backpressure support
    /// - Cancellation support
    /// </remarks>
    IAsyncEnumerable<TResponse> Stream<TResponse>(INexStreamRequest<TResponse> request, CancellationToken cancellationToken = default);
}
