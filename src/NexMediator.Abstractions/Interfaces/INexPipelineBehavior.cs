namespace NexMediator.Abstractions.Interfaces;

/// <summary>
/// Defines a behavior that can be added to the request processing pipeline
/// </summary>
/// <typeparam name="TRequest">Type of request being processed</typeparam>
/// <typeparam name="TResponse">Type of response expected</typeparam>
/// <remarks>
/// Pipeline concepts:
/// - Behaviors wrap request handling
/// - Execute in defined order
/// - Can modify request/response
/// - Can short-circuit pipeline
/// 
/// Common uses:
/// - Validation
/// - Caching
/// - Transactions
/// - Logging
/// - Performance monitoring
/// - Error handling
/// </remarks>
public interface INexPipelineBehavior<in TRequest, TResponse>
    where TRequest : INexRequest<TResponse>
{
    /// <summary>
    /// Handles a step in the pipeline
    /// </summary>
    /// <param name="request">Request being processed</param>
    /// <param name="next">Delegate to execute next behavior or handler</param>
    /// <param name="cancellationToken">Optional cancellation token</param>
    /// <returns>Response from pipeline</returns>
    /// <remarks>
    /// Implementation notes:
    /// - Must call next() to continue pipeline
    /// - Can modify request before next()
    /// - Can modify response after next()
    /// - Can skip pipeline via early return
    /// - Should handle cancellation
    /// - Must propagate exceptions
    /// </remarks>
    Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken);
}

/// <summary>
/// Delegate for the next pipeline step
/// </summary>
/// <typeparam name="TResponse">Response type</typeparam>
/// <remarks>
/// Represents:
/// - Next behavior or
/// - Final handler
/// </remarks>
public delegate Task<TResponse> RequestHandlerDelegate<TResponse>();
