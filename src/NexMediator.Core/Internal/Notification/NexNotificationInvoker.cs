using Microsoft.Extensions.DependencyInjection;
using NexMediator.Abstractions.Interfaces;

namespace NexMediator.Core.Internal.Notification;

/// <summary>
/// Invokes all registered handlers for a given notification type.
/// </summary>
/// <typeparam name="TNotification">The notification type.</typeparam>
internal sealed class NexNotificationInvoker<TNotification> : INexNotificationInvoker
    where TNotification : INexNotification
{
    /// <summary>
    /// Executes all handlers registered for the specified notification.
    /// </summary>
    public async Task Invoke(object notification, IServiceProvider provider, CancellationToken cancellationToken)
    {
        var handlers = provider.GetServices<INexNotificationHandler<TNotification>>();

        var tasks = new List<Task>(capacity: 4);
        foreach (var handler in handlers)
        {
            tasks.Add(handler.Handle((TNotification)notification, cancellationToken));
        }

        await Task.WhenAll(tasks).ConfigureAwait(false);
    }
}

