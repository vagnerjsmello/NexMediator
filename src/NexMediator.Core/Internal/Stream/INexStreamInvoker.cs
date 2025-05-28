namespace NexMediator.Core.Internal.Stream;

/// <summary>
/// Represents a strongly-typed stream request invoker for a specific request/response pair.
/// </summary>
/// <typeparam name="TRequest">Type of the stream request.</typeparam>
/// <typeparam name="TResponse">Type of the response item.</typeparam>
internal interface INexStreamInvoker
{
    /// <summary>
    /// Invokes the handler for the given stream request.
    /// </summary>
    /// <param name="request">The stream request instance.</param>
    /// <param name="provider">The service provider to resolve dependencies.</param>
    /// <param name="ct">Cancellation token for cooperative cancellation.</param>
    /// <returns>An async stream of response items.</returns>
    IAsyncEnumerable<object> Invoke(object request, IServiceProvider provider, CancellationToken ct);
}
