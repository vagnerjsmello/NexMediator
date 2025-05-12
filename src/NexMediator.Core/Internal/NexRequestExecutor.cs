using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using NexMediator.Abstractions.Exceptions;
using NexMediator.Abstractions.Interfaces;

namespace NexMediator.Core.Internal;

/// <summary>
/// Builds and caches a pipeline invoker per request and response type.
/// Supports strong typing of behaviors and correct ordering.
/// </summary>
internal static class NexRequestExecutor
{
    private static readonly ConcurrentDictionary<
        (Type Request, Type Response),
        Func<object, CancellationToken, Task<object>>>
      _pipelineCache = new();

    /// <summary>
    /// Dispatches a request through a cached pipeline and returns the typed response.
    /// </summary>
    /// <typeparam name="TResponse">The expected response type.</typeparam>
    /// <param name="request">The request to send.</param>
    /// <param name="provider">The root service provider.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The response from the handler.</returns>
    /// <exception cref="ArgumentNullException">Thrown if request is null.</exception>
    public static async Task<TResponse> Dispatch<TResponse>(
        INexRequest<TResponse> request,
        IServiceProvider provider,
        CancellationToken cancellationToken)
    {
        if (request is null)
            throw new ArgumentNullException(nameof(request));

        var reqType = request.GetType();
        var resType = typeof(TResponse);
        var key = (reqType, resType);

        var invoker = _pipelineCache.GetOrAdd(
            key,
            _ => BuildFullPipelineInvoker(reqType, resType, provider));

        var result = await invoker(request!, cancellationToken)
                           .ConfigureAwait(false);
        return (TResponse)result!;
    }

    /// <summary>
    /// Builds a generic pipeline invoker by reflecting to a generic method.
    /// </summary>
    private static Func<object, CancellationToken, Task<object>>
    BuildFullPipelineInvoker(
        Type reqType,
        Type resType,
        IServiceProvider provider)
    {
        var method = typeof(NexRequestExecutor)
            .GetMethod(nameof(BuildPipelineGeneric), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!;
        var generic = method.MakeGenericMethod(reqType, resType);
        return (Func<object, CancellationToken, Task<object>>)
            generic.Invoke(null, new object[] { provider })!;
    }

    /// <summary>
    /// Builds a strongly typed pipeline for TRequest and TResponse.
    /// </summary>
    private static Func<object, CancellationToken, Task<object>>
    BuildPipelineGeneric<TRequest, TResponse>(IServiceProvider provider)
        where TRequest : INexRequest<TResponse>
    {
        var scopeFactory = provider.GetRequiredService<IServiceScopeFactory>();

        return async (object reqObj, CancellationToken ct) =>
        {
            using var scope = scopeFactory.CreateScope();
            var sp = scope.ServiceProvider;

            // Resolve options and handler
            var options = sp.GetRequiredService<NexMediatorOptions>();
            var handler = sp.GetRequiredService<INexRequestHandler<TRequest, TResponse>>();

            // Base pipeline calls the handler
            async Task<TResponse> HandlerPipe() =>
                await handler.Handle((TRequest)reqObj, ct)
                             .ConfigureAwait(false);

            RequestHandlerDelegate<TResponse> pipeline = HandlerPipe;

            // Resolve, order, and wrap behaviors
            var behaviors = sp.GetServices<INexPipelineBehavior<TRequest, TResponse>>();
            var ordered = options.OrderBehaviors(behaviors.Cast<object>());

            foreach (var behaviorObj in ordered.Reverse())
            {
                var behavior = (INexPipelineBehavior<TRequest, TResponse>)behaviorObj;
                var next = pipeline;

                async Task<TResponse> BehaviorPipe() =>
                    await behavior.Handle((TRequest)reqObj, next, ct)
                                  .ConfigureAwait(false);

                pipeline = BehaviorPipe;
            }

            // Execute and return
            var response = await pipeline().ConfigureAwait(false);
            if (response is null)
                throw new NexMediatorException("Handler returned null response.");

            return (object)response!;
        };
    }
}
