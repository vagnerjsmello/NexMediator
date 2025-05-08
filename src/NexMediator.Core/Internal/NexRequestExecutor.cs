using Microsoft.Extensions.DependencyInjection;
using NexMediator.Abstractions.Exceptions;
using NexMediator.Abstractions.Interfaces;
using System.Collections.Concurrent;
using System.Linq.Expressions;

namespace NexMediator.Core.Internal;

/// <summary>
/// NexRequestExecutor builds and caches delegates to avoid reflection at runtime.
/// </summary>
internal static class NexRequestExecutor
{
    // Cache delegates for each request-response type pair
    private static readonly ConcurrentDictionary<(Type requestType, Type responseType), Func<object, IServiceProvider, CancellationToken, Task<object>>> _cache = new();

    /// <summary>
    /// Dispatch the request using a compiled delegate from the cache.
    /// </summary>
    public static async Task<TResponse> Dispatch<TResponse>(
        INexRequest<TResponse> request,
        IServiceProvider provider,
        CancellationToken cancellationToken)
    {
        var requestType = request.GetType();
        var key = (requestType, typeof(TResponse));

        // Get or build a delegate to handle the request
        var executor = _cache.GetOrAdd(key, static tuple =>
        {
            var (reqType, resType) = tuple;

            // Get method: Execute<TRequest, TResponse>
            var executeMethod = typeof(NexRequestExecutor)
                .GetMethod(nameof(Execute))!
                .MakeGenericMethod(reqType, resType);

            // Create parameters: (object request, IServiceProvider provider, CancellationToken ct)
            var requestParam = Expression.Parameter(typeof(object), "request");
            var providerParam = Expression.Parameter(typeof(IServiceProvider), "provider");
            var tokenParam = Expression.Parameter(typeof(CancellationToken), "ct");

            // Cast the request to its real type
            var castedRequest = Expression.Convert(requestParam, reqType);

            // Call Execute<TRequest, TResponse>(typedRequest, provider, ct)
            var callExecute = Expression.Call(executeMethod, castedRequest, providerParam, tokenParam);

            // Get method WrapTask<T>(Task<T>) -> Task<object>
            var wrapMethod = typeof(NexRequestExecutor)
                .GetMethods()
                .First(m =>
                    m.Name == nameof(WrapTask)
                    && m.IsGenericMethodDefinition
                    && m.GetGenericArguments().Length == 1
                    && m.GetParameters()[0].ParameterType.IsGenericType
                    && m.GetParameters()[0].ParameterType.GetGenericTypeDefinition() == typeof(Task<>)
                )
                .MakeGenericMethod(resType);

            // Wrap the result of Execute in a Task<object>
            var wrappedCall = Expression.Call(wrapMethod, callExecute);

            // Final delegate: (object, IServiceProvider, CancellationToken) => WrapTask(Execute(...))
            var lambda = Expression.Lambda<Func<object, IServiceProvider, CancellationToken,
                Task<object>>>(wrappedCall, requestParam, providerParam, tokenParam);

            return lambda.Compile();
        });

        // Run the compiled delegate
        var result = await executor(request, provider, cancellationToken);
        return (TResponse)result!;
    }

    /// <summary>
    /// Converts a Task T into Task object to unify return types.
    /// </summary>
    public static async Task<object> WrapTask<T>(Task<T> task)
    {
        var result = await task.ConfigureAwait(false);
        return result ?? throw new NexMediatorException("Handler method returned null.");
    }

    /// <summary>
    /// Executes the request handler and applies pipeline behaviors.
    /// Ensures all handler resolution occurs within a scoped service provider
    /// to support dependencies that have scoped lifetimes (like repositories).
    /// </summary>
    public static Task<TResponse> Execute<TRequest, TResponse>(
        TRequest request,
        IServiceProvider provider,
        CancellationToken cancellationToken)
        where TRequest : INexRequest<TResponse>
    {
        // Create a new scope to resolve scoped dependencies like repositories
        using var scope = provider.CreateScope();
        var scopedProvider = scope.ServiceProvider;

        // Resolve the request handler within scope
        var handler = scopedProvider.GetRequiredService<INexRequestHandler<TRequest, TResponse>>();

        // Resolve all pipeline behaviors (optional, ordered) within the same scope
        var behaviors = scopedProvider
            .GetServices<INexPipelineBehavior<TRequest, TResponse>>()
            .Where(b => b != null)
            .ToList();

        // Create the final delegate by chaining all behaviors and the base handler
        RequestHandlerDelegate<TResponse> handlerDelegate = () => handler.Handle(request, cancellationToken);

        foreach (var behavior in Enumerable.Reverse(behaviors))
        {
            var next = handlerDelegate;
            handlerDelegate = () => behavior.Handle(request, next, cancellationToken);
        }

        return handlerDelegate();
    }
}
