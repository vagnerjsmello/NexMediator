namespace NexMediator.Core.Internal.Notification;

/// <summary>
/// Defines a contract for invoking all handlers of a notification.
/// </summary>
internal interface INexNotificationInvoker
{
    /// <summary>
    /// Executes all notification handlers asynchronously.
    /// </summary>
    Task Invoke(object notification, IServiceProvider provider, CancellationToken cancellationToken);
}

