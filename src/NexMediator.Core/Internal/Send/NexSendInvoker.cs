using Microsoft.Extensions.DependencyInjection;
using NexMediator.Abstractions.Exceptions;
using NexMediator.Abstractions.Interfaces;

namespace NexMediator.Core.Internal.Send;

/// <summary>
/// Provides a strongly-typed implementation of the request pipeline executor.
/// </summary>
/// <typeparam name="TRequest">The type of request being handled.</typeparam>
/// <typeparam name="TResponse">The type of response expected from the handler.</typeparam>
internal sealed class NexSendInvoker<TRequest, TResponse> : INexSendInvoker
    where TRequest : INexRequest<TResponse>
{
    /// <summary>
    /// Executes the request pipeline using resolved behaviors and handler.
    /// </summary>
    /// <param name="request">The request object.</param>
    /// <param name="provider">The service provider used to resolve dependencies.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The result of the request execution as an object.</returns>
    public async Task<object> Invoke(object request, IServiceProvider provider, CancellationToken cancellationToken)
    {
        var scopeFactory = provider.GetRequiredService<IServiceScopeFactory>();
        using var scope = scopeFactory.CreateScope();
        var sp = scope.ServiceProvider;

        var options = sp.GetRequiredService<NexMediatorOptions>();
        var handler = sp.GetRequiredService<INexRequestHandler<TRequest, TResponse>>();

        async Task<TResponse> HandlerPipe() =>
            await handler.Handle((TRequest)request, cancellationToken).ConfigureAwait(false);

        RequestHandlerDelegate<TResponse> pipeline = HandlerPipe;

        // Check if any behaviors are registered before applying them
        var behaviors = sp.GetServices<INexPipelineBehavior<TRequest, TResponse>>();
        if (behaviors.Any())
        {
            var ordered = options.OrderBehaviors(behaviors.Cast<object>());

            foreach (var behaviorObj in ordered.Reverse())
            {
                var behavior = (INexPipelineBehavior<TRequest, TResponse>)behaviorObj;
                var next = pipeline;

                async Task<TResponse> BehaviorPipe() =>
                    await behavior.Handle((TRequest)request, next, cancellationToken).ConfigureAwait(false);

                pipeline = BehaviorPipe;
            }
        }

        var response = await pipeline().ConfigureAwait(false);
        if (response is null)
            throw new NexMediatorException("Handler returned null response.");

        return response!;
    }
}
