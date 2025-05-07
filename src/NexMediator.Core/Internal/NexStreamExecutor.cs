using Microsoft.Extensions.DependencyInjection;
using NexMediator.Abstractions.Interfaces;
using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;

namespace NexMediator.Core.Internal;

/// <summary>
/// Executes a stream request using compiled delegate handlers.
///
/// Optimization:
/// - Uses compiled expressions to avoid runtime reflection.
/// - Caches dispatcher delegates per (request type, response type).
/// - Eliminates dynamic casting overhead.
/// </summary>
internal static class NexStreamExecutor
{
    private static readonly ConcurrentDictionary<(Type requestType, Type responseType), Func<object, IServiceProvider, CancellationToken, IAsyncEnumerable<object>>> _cache = new();

    /// <summary>
    /// Executes the stream request and returns the async response stream.
    /// </summary>
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

            // typeof(INexStreamRequestHandler<TRequest, TResponse>)
            var handlerType = typeof(INexStreamRequestHandler<,>).MakeGenericType(reqType, resType);

            // Method: handler.Handle(request, ct)
            var handleMethod = handlerType.GetMethod(nameof(INexStreamRequestHandler<INexStreamRequest<object>, object>.Handle))!;

            // Parameters
            var requestParam = Expression.Parameter(typeof(object), "request");
            var providerParam = Expression.Parameter(typeof(IServiceProvider), "provider");
            var ctParam = Expression.Parameter(typeof(CancellationToken), "ct");

            // Cast request to TRequest
            var castedRequest = Expression.Convert(requestParam, reqType);

            // provider.GetRequiredService<INexStreamRequestHandler<TRequest, TResponse>>()
            var getHandlerMethod = typeof(ServiceProviderServiceExtensions)
                .GetMethod(nameof(ServiceProviderServiceExtensions.GetRequiredService), new[] { typeof(IServiceProvider) })!
                .MakeGenericMethod(handlerType);

            var getHandlerExpr = Expression.Call(getHandlerMethod, providerParam);

            // handler.Handle(castedRequest, ct)
            var callHandleExpr = Expression.Call(
                getHandlerExpr,
                handleMethod,
                castedRequest,
                ctParam
            );

            // Wrap result to IAsyncEnumerable<object>
            var convertResult = Expression.Convert(callHandleExpr, typeof(IAsyncEnumerable<object>));

            var lambda = Expression.Lambda<Func<object, IServiceProvider, CancellationToken, IAsyncEnumerable<object>>>(
                convertResult,
                requestParam,
                providerParam,
                ctParam
            );

            return lambda.Compile();
        });

        var result = executor(request!, provider, cancellationToken);

        // Cast to IAsyncEnumerable<TResponse>
        return CastAsync<TResponse>(result, cancellationToken);
    }

    /// <summary>
    /// Casts async stream items from object to TResponse.
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
