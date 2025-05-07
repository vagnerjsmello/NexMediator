namespace NexMediator.Abstractions.Interfaces;

/// <summary>
/// Manages transactions for unit of work operations
/// </summary>
/// <remarks>
/// Core features:
/// - Transaction lifecycle
/// - Nested transactions
/// - Resource management
/// - Error handling
/// </remarks>
public interface ITransactionManager
{
    /// <summary>
    /// Starts a new transaction
    /// </summary>
    /// <param name="cancellationToken">Optional cancellation token</param>
    /// <returns>Task representing completion</returns>
    /// <remarks>
    /// Actions:
    /// - Create transaction scope
    /// - Set isolation level
    /// - Handle nesting
    /// </remarks>
    Task BeginTransactionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Commits current transaction
    /// </summary>
    /// <param name="cancellationToken">Optional cancellation token</param>
    /// <returns>Task representing completion</returns>
    /// <remarks>
    /// Actions:
    /// - Validate state
    /// - Commit changes
    /// - Clean resources
    /// </remarks>
    Task CommitTransactionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Rolls back current transaction
    /// </summary>
    /// <param name="cancellationToken">Optional cancellation token</param>
    /// <returns>Task representing completion</returns>
    /// <remarks>
    /// Actions:
    /// - Revert changes
    /// - Log failures
    /// - Clean resources
    /// </remarks>
    Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
}