namespace NexMediator.Abstractions.Interfaces;

/// <summary>
/// Defines handler for notification processing in the mediator pipeline
/// </summary>
/// <typeparam name="TNotification">The notification type</typeparam>
/// <remarks>
/// Key aspects:
/// - Multiple handlers per notification
/// - Independent execution
/// - Fault isolation
/// - No return values
/// 
/// Common uses:
/// - Email notifications
/// - Audit logging
/// - Cache updates
/// - Event processing
/// </remarks>
public interface INexNotificationHandler<in TNotification>
    where TNotification : INexNotification
{
    /// <summary>
    /// Processes a notification
    /// </summary>
    /// <param name="notification">The notification</param>
    /// <param name="cancellationToken">Optional cancellation token</param>
    /// <returns>Task representing completion</returns>
    /// <remarks>
    /// Guidelines:
    /// - Handle exceptions internally
    /// - Keep processing focused
    /// - Support cancellation
    /// - Log key events
    /// </remarks>
    Task Handle(TNotification notification, CancellationToken cancellationToken);
}
