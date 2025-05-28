using NexMediator.Abstractions.Interfaces;
using System.Collections.Concurrent;

namespace NexMediator.Core.Internal.Send;

/// <summary>
/// Executes a request pipeline using a cached, strongly-typed invoker per request and response type.
/// This version avoids reflection in the hot path by using generic invoker wrappers.
/// </summary>
internal static class NexSendExecutor
{
    private static readonly ConcurrentDictionary<(Type Request, Type Response), INexSendInvoker> _invokerCache = new();

    /// <summary>
    /// Dispatches a request through a cached invoker and returns the typed response.
    /// </summary>
    /// <typeparam name="TResponse">The expected response type.</typeparam>
    /// <param name="request">The request to execute.</param>
    /// <param name="provider">The root service provider.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The response returned from the request pipeline.</returns>
    /// <exception cref="ArgumentNullException">Thrown if the request is null.</exception>
    public static async Task<TResponse> Dispatch<TResponse>(
        INexRequest<TResponse> request,
        IServiceProvider provider,
        CancellationToken cancellationToken)
    {
        if (request is null)
            throw new ArgumentNullException(nameof(request));

        var requestType = request.GetType();
        var responseType = typeof(TResponse);
        var key = (requestType, responseType);

        var invoker = _invokerCache.GetOrAdd(key, static k =>
        {
            var wrapperType = typeof(NexSendInvoker<,>).MakeGenericType(k.Request, k.Response);
            return (INexSendInvoker)Activator.CreateInstance(wrapperType)!;
        });

        var result = await invoker.Invoke(request!, provider, cancellationToken).ConfigureAwait(false);
        return (TResponse)result!;
    }
}
