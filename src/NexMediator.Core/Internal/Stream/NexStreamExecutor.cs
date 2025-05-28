using Microsoft.Extensions.DependencyInjection;
using NexMediator.Abstractions.Interfaces;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;

namespace NexMediator.Core.Internal.Stream;

/// <summary>
/// Executes stream requests using compiled delegate wrappers per request/response types.
/// </summary>
internal static class NexStreamExecutor
{
    private static readonly ConcurrentDictionary<(Type, Type), INexStreamInvoker> _cache = new();

    /// <summary>
    /// Dispatches a stream request using a cached invoker wrapper.
    /// </summary>
    /// <typeparam name="TResponse">The type of each response item.</typeparam>
    /// <param name="request">The stream request to execute.</param>
    /// <param name="provider">The service provider to resolve handlers.</param>
    /// <param name="cancellationToken">Token used to cancel the async stream.</param>
    /// <returns>An async stream of responses.</returns>
    public static IAsyncEnumerable<TResponse> Execute<TResponse>(
        INexStreamRequest<TResponse> request,
        IServiceProvider provider,
        CancellationToken cancellationToken)
    {
        var key = (request.GetType(), typeof(TResponse));

        // Local function avoids static lambda limitation with provider
        INexStreamInvoker CreateInvoker((Type, Type) tuple)
        {
            var wrapperType = typeof(NexStreamInvoker<,>).MakeGenericType(tuple.Item1, tuple.Item2);
            return (INexStreamInvoker)ActivatorUtilities.CreateInstance(provider, wrapperType)!;
        }

        var invoker = _cache.GetOrAdd(key, CreateInvoker);

        var stream = invoker.Invoke(request, provider, cancellationToken);
        return CastAsync<TResponse>(stream, cancellationToken);
    }

    /// <summary>
    /// Casts the response stream from object to the expected type.
    /// </summary>
    private static async IAsyncEnumerable<TResponse> CastAsync<TResponse>(
        IAsyncEnumerable<object> source,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        await foreach (var item in source.WithCancellation(ct).ConfigureAwait(false))
        {
            yield return (TResponse)item!;
        }
    }
}
