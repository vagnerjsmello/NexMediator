namespace NexMediator.Abstractions.Interfaces;

/// <summary>
/// Processes requests before they reach handlers
/// </summary>
/// <typeparam name="TRequest">Request type</typeparam>
/// <remarks>
/// Common uses:
/// - Request enrichment
/// - Security checks
/// - Validation
/// - Logging setup
/// - Context preparation
/// </remarks>
public interface INexRequestPreProcessor<in TRequest>
{
    /// <summary>
    /// Pre-processes a request
    /// </summary>
    /// <param name="request">Request to process</param>
    /// <param name="cancellationToken">Optional cancellation token</param>
    /// <returns>Task representing completion</returns>
    /// <remarks>
    /// Guidelines:
    /// - Keep processing light
    /// - Handle failures early
    /// - Check cancellation
    /// - Document changes
    /// </remarks>
    Task Process(TRequest request, CancellationToken cancellationToken);
}
