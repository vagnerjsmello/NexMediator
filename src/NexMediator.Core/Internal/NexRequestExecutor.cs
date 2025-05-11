using Microsoft.Extensions.DependencyInjection;
using NexMediator.Abstractions.Exceptions;
using NexMediator.Abstractions.Interfaces;
using System.Collections.Concurrent;

namespace NexMediator.Core.Internal;

/// <summary>
/// Builds and caches a pipeline invoker per request and response type.
/// The pipeline:
/// 1. Creates a service scope.
/// 2. Resolves handler and behaviors.
/// 3. Chains behaviors around the handler.
/// 4. Executes the pipeline and returns the response as object.
/// </summary>
internal static class NexRequestExecutor
{
    private static readonly ConcurrentDictionary<(Type Request, Type Response), Func<object, CancellationToken, Task<object>>> _pipelineCache = new();

    /// <summary>
    /// Dispatches a request through a cached pipeline and returns the typed response.
    /// </summary>
    /// <typeparam name="TResponse">The expected response type.</typeparam>
    /// <param name="request">The request to send.</param>
    /// <param name="provider">The root service provider.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The response from the handler.</returns>
    /// <exception cref="ArgumentNullException">Thrown if request is null.</exception>
    public static async Task<TResponse> Dispatch<TResponse>(INexRequest<TResponse> request, IServiceProvider provider, CancellationToken cancellationToken)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));

        var reqType = request.GetType();
        var resType = typeof(TResponse);
        var key = (Request: reqType, Response: resType);

        var invoker = _pipelineCache.GetOrAdd(key, _ => BuildFullPipelineInvoker(reqType, resType, provider));

        var resultObj = await invoker(request, cancellationToken).ConfigureAwait(false);
        return (TResponse)resultObj!;
    }

    /// <summary>
    /// Builds a delegate that runs the full pipeline for a given request and response types.
    /// </summary>
    /// <param name="reqType">The runtime type of the request.</param>
    /// <param name="resType">The runtime type of the response.</param>
    /// <param name="rootProvider">The root service provider for scopes.</param>
    /// <returns>A function that processes a request and returns the result boxed as object.</returns>
    private static Func<object, CancellationToken, Task<object>> BuildFullPipelineInvoker(Type reqType, Type resType, IServiceProvider rootProvider)
    {
        var scopeFactory = rootProvider.GetRequiredService<IServiceScopeFactory>();

        var handlerInterface = typeof(INexRequestHandler<,>).MakeGenericType(reqType, resType);
        Func<IServiceProvider, object> handlerFactory = sp => sp.GetRequiredService(handlerInterface)!;

        var behaviorInterface = typeof(INexPipelineBehavior<,>).MakeGenericType(reqType, resType);


        var behaviorFactories = rootProvider
            .GetServices(behaviorInterface)
            .Select(b => b!.GetType())
            .Select(type => (Func<IServiceProvider, object>)(sp => sp.GetRequiredService(type)!))
            .ToArray();

        return async (object reqObj, CancellationToken ct) =>
        {
            using var scope = scopeFactory.CreateScope();
            var sp = scope.ServiceProvider;

            dynamic handler = handlerFactory(sp);
            Func<Task<object>> pipeline = async () =>
            {
                var response = await handler.Handle((dynamic)reqObj, ct).ConfigureAwait(false);
                return (object)response!;
            };

            for (int i = behaviorFactories.Length - 1; i >= 0; i--)
            {
                dynamic behavior = behaviorFactories[i](sp);
                var next = pipeline;
                pipeline = async () =>
                {
                    var response = await behavior.Handle((dynamic)reqObj, next, ct).ConfigureAwait(false);
                    return (object)response!;
                };
            }

            var finalResult = await pipeline().ConfigureAwait(false);

            if (finalResult == null)
                throw new NexMediatorException("Handler returned null response.");

            return finalResult;
        };
    }
}
