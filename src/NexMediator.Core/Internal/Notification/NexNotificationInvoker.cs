using Microsoft.Extensions.DependencyInjection;
using NexMediator.Abstractions.Interfaces;

namespace NexMediator.Core.Internal.Notification;

/// <summary>
/// Invokes all registered handlers for a given notification type.
/// Supports parallel or sequential execution based on options or per-call override.
/// </summary>
/// <typeparam name="TNotification">The notification type.</typeparam>
internal sealed class NexNotificationInvoker<TNotification> : INexNotificationInvoker
    where TNotification : INexNotification
{
    /// <summary>
    /// Executes all handlers registered for the specified notification type.
    /// Can run in parallel or sequence depending on configuration.
    /// </summary>
    /// <param name="notification">The notification object to dispatch.</param>
    /// <param name="provider">The service provider to resolve handlers and options.</param>
    /// <param name="cancellationToken">Token to cancel the handler execution.</param>
    /// <param name="parallelExecutionOverride">
    /// If true, runs handlers in parallel. If false, runs them in order.
    /// If null, uses global option defined in <see cref="NexMediatorOptions"/>.
    /// </param>
    public async Task Invoke(
    object notification,
    IServiceProvider provider,
    CancellationToken cancellationToken,
    bool? parallelExecutionOverride = null)
    {
        var handlers = provider.GetServices<INexNotificationHandler<TNotification>>();
        var options = provider.GetRequiredService<NexMediatorOptions>();
        var runInParallel = parallelExecutionOverride ?? options.PublishNotificationsInParallel;

        if (runInParallel)
            await InvokeInParallel(notification, handlers, cancellationToken).ConfigureAwait(false);
        else
            await InvokeSequentially(notification, handlers, cancellationToken).ConfigureAwait(false);
    }

    private static async Task InvokeInParallel(object notification, IEnumerable<INexNotificationHandler<TNotification>> handlers, CancellationToken cancellationToken)
    {
        var handlerList = handlers.ToList();
        int count = handlerList.Count;

        if (count == 1)
        {
            // Execute the only registered handler directly without using Task.WhenAll
            await handlerList[0].Handle((TNotification)notification, cancellationToken).ConfigureAwait(false);
            return;
        }

        if (count > 1)
        {
            var tasks = new Task[count];
            for (int i = 0; i < count; i++)
            {
                tasks[i] = handlerList[i].Handle((TNotification)notification, cancellationToken);
            }

            await Task.WhenAll(tasks).ConfigureAwait(false);
        }
    }

    private static async Task InvokeSequentially(object notification, IEnumerable<INexNotificationHandler<TNotification>> handlers, CancellationToken cancellationToken)
    {
        foreach (var handler in handlers)
        {
            await handler.Handle((TNotification)notification, cancellationToken).ConfigureAwait(false);
        }
    }

}
