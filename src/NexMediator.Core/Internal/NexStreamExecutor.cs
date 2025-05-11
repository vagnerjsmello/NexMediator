using Microsoft.Extensions.DependencyInjection;
using NexMediator.Abstractions.Interfaces;
using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;

namespace NexMediator.Core.Internal;

/// <summary>
/// Executes stream requests using compiled delegates per request/response types.
/// 
/// Optimizations:
/// - Compiles expressions to avoid reflection at runtime.
/// - Caches executor delegates by request and response types.
/// - Avoids dynamic casting overhead.
/// </summary>
internal static class NexStreamExecutor
{
    private static readonly ConcurrentDictionary<(Type requestType, Type responseType),
        Func<object, IServiceProvider, CancellationToken, IAsyncEnumerable<object>>> _cache =
        new();

    /// <summary>
    /// Executes a stream request and returns an async sequence of responses.
    /// </summary>
    /// <typeparam name="TResponse">Type of each response item.</typeparam>
    /// <param name="request">The stream request to execute.</param>
    /// <param name="provider">Service provider for resolving the handler.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>An async enumerable of <typeparamref name="TResponse"/> items.</returns>
    public static IAsyncEnumerable<TResponse> Execute<TResponse>(
        INexStreamRequest<TResponse> request,
        IServiceProvider provider,
        CancellationToken cancellationToken)
    {
        var requestType = request.GetType();
        var key = (requestType, typeof(TResponse));

        var executor = _cache.GetOrAdd(key, static tuple =>
        {
            var (reqType, resType) = tuple;
            var handlerType = typeof(INexStreamRequestHandler<,>)
                .MakeGenericType(reqType, resType);
            var handleMethod = handlerType
                .GetMethod(nameof(INexStreamRequestHandler<INexStreamRequest<object>, object>.Handle))!;

            var requestParam = Expression.Parameter(typeof(object), "request");
            var providerParam = Expression.Parameter(typeof(IServiceProvider), "provider");
            var ctParam = Expression.Parameter(typeof(CancellationToken), "ct");

            var castRequest = Expression.Convert(requestParam, reqType);
            var getHandler = Expression.Call(
                typeof(ServiceProviderServiceExtensions),
                nameof(ServiceProviderServiceExtensions.GetRequiredService),
                new[] { handlerType },
                providerParam);

            var callHandle = Expression.Call(
                getHandler,
                handleMethod,
                castRequest,
                ctParam);

            var convert = Expression.Convert(
                callHandle,
                typeof(IAsyncEnumerable<object>));

            var lambda = Expression.Lambda<Func<object, IServiceProvider, CancellationToken, IAsyncEnumerable<object>>>(
                convert,
                requestParam,
                providerParam,
                ctParam);

            return lambda.Compile();
        });

        var result = executor(request!, provider, cancellationToken);
        return CastAsync<TResponse>(result, cancellationToken);
    }

    /// <summary>
    /// Casts each object item in an async sequence to <typeparamref name="TResponse"/>.
    /// </summary>
    /// <typeparam name="TResponse">Target item type.</typeparam>
    /// <param name="source">Source sequence of objects.</param>
    /// <param name="ct">Token to cancel the iteration.</param>
    /// <returns>An async enumerable of <typeparamref name="TResponse"/> items.</returns>
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
