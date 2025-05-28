namespace NexMediator.Core.Internal.Notification;

/// <summary>
/// Defines a contract for invoking all handlers of a notification.
/// </summary>
internal interface INexNotificationInvoker
{
    /// <summary>
    /// Executes all notification handlers asynchronously.
    /// </summary>
    /// <param name="parallelExecutionOverride">
    /// If true, handlers run in parallel. If false, they run in sequence.
    /// If null, default from NexMediatorOptions is used.
    /// </param>
    Task Invoke(object notification, IServiceProvider provider, CancellationToken cancellationToken, bool? parallelExecutionOverride = null);

}

