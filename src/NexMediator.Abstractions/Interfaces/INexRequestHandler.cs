namespace NexMediator.Abstractions.Interfaces;

/// <summary>
/// Defines handler for processing requests in the pipeline
/// </summary>
/// <typeparam name="TRequest">Request type</typeparam>
/// <typeparam name="TResponse">Response type</typeparam>
/// <remarks>
/// Responsibilities:
/// - Execute business logic
/// - Process requests
/// - Return responses
/// - Handle errors
/// 
/// Best practices:
/// - Single responsibility
/// - Dependency injection
/// - Error handling
/// - Resource cleanup
/// </remarks>
public interface INexRequestHandler<in TRequest, TResponse>
    where TRequest : INexRequest<TResponse>
{
    /// <summary>
    /// Processes a request
    /// </summary>
    /// <param name="request">Request to handle</param>
    /// <param name="cancellationToken">Optional cancellation token</param>
    /// <returns>Response from handling</returns>
    /// <remarks>
    /// Guidelines:
    /// - Validate inputs
    /// - Handle cancellation
    /// - Log key events
    /// - Dispose resources
    /// </remarks>
    Task<TResponse> Handle(TRequest request, CancellationToken cancellationToken);
}
