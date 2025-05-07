namespace NexMediator.Abstractions.Interfaces;

/// <summary>
/// Handles streaming requests in the pipeline
/// </summary>
/// <typeparam name="TRequest">Request type</typeparam>
/// <typeparam name="TResponse">Response item type</typeparam>
/// <remarks>
/// Key features:
/// - Async enumerable results
/// - Incremental processing
/// - Resource efficiency
/// - Cancellation support
/// </remarks>
public interface INexStreamRequestHandler<in TRequest, TResponse>
    where TRequest : INexStreamRequest<TResponse>
{
    /// <summary>
    /// Handles streaming request
    /// </summary>
    /// <param name="request">Request to process</param>
    /// <param name="cancellationToken">Optional cancellation token</param>
    /// <returns>Stream of responses</returns>
    /// <remarks>
    /// Guidelines:
    /// - Yield results early
    /// - Check cancellation
    /// - Clean up resources
    /// - Handle backpressure
    /// </remarks>
    IAsyncEnumerable<TResponse> Handle(TRequest request, CancellationToken cancellationToken);
}
