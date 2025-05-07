namespace NexMediator.Abstractions.Interfaces;

/// <summary>
/// Marker interface for notifications in the mediator pipeline
/// </summary>
/// <remarks>
/// Characteristics:
/// - Multiple handlers allowed
/// - No return values
/// - Parallel processing
/// - Independent execution
/// 
/// Common uses:
/// - Event notifications
/// - System events
/// - Audit logging
/// - Cross-boundary messaging
/// </remarks>
public interface INexNotification { }
