using NexMediator.Abstractions.Interfaces;
using System.Collections.Concurrent;

namespace NexMediator.Core.Internal.Notification;

/// <summary>
/// Builds and caches a dispatcher for each notification type.
/// Uses invoker wrappers to execute handlers, avoiding reflection on the hot path.
/// </summary>
internal static class NexNotificationExecutor
{
    private static readonly ConcurrentDictionary<Type, INexNotificationInvoker> _cache = new();

    /// <summary>
    /// Publishes a notification by invoking all handlers.
    /// Execution mode (parallel/sequential) is resolved via options or overridden.
    /// </summary>
    /// <typeparam name="TNotification">Notification type.</typeparam>
    /// <param name="notification">The notification to publish.</param>
    /// <param name="provider">Service provider to resolve handlers and options.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <param name="parallelExecution">
    /// Optional override. If true, handlers run in parallel. If false, in sequence.
    /// If null, uses <see cref="NexMediatorOptions.PublishNotificationsInParallel"/>.
    /// </param>
    public static Task Publish<TNotification>(
        TNotification notification,
        IServiceProvider provider,
        CancellationToken cancellationToken,
        bool? parallelExecution = null)
        where TNotification : INexNotification
    {
        if (notification == null)
            throw new ArgumentNullException(nameof(notification));

        var notificationType = typeof(TNotification);

        var invoker = _cache.GetOrAdd(notificationType, static type =>
        {
            var wrapperType = typeof(NexNotificationInvoker<>).MakeGenericType(type);
            return (INexNotificationInvoker)Activator.CreateInstance(wrapperType)!;
        });

        return invoker.Invoke(notification!, provider, cancellationToken, parallelExecution);
    }
}
