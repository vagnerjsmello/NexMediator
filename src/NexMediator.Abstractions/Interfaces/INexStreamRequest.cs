namespace NexMediator.Abstractions.Interfaces;

/// <summary>
/// Request that returns an async stream of responses
/// </summary>
/// <typeparam name="TResponse">Response item type</typeparam>
/// <remarks>
/// Use cases:
/// - Large datasets
/// - Real-time feeds
/// - Memory-efficient processing
/// - Long-running operations
/// 
/// Features:
/// - Async streaming
/// - Backpressure
/// - Cancellation support
/// </remarks>
public interface INexStreamRequest<out TResponse> : INexRequest<IAsyncEnumerable<TResponse>>
{
}
