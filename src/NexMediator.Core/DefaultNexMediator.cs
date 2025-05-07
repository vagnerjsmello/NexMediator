using Microsoft.Extensions.Logging;
using NexMediator.Abstractions.Interfaces;
using NexMediator.Core.Internal;

namespace NexMediator.Core;

/// <summary>
/// Default implementation of INexMediator that coordinates sending requests and publishing notifications
/// </summary>
/// <remarks>
/// Core capabilities:
/// - Request routing and handling
/// - Pipeline behavior management
/// - Notification publishing
/// - Stream request processing
/// 
/// Processing sequence:
/// 1. Validation and preprocessing
/// 2. Pipeline behavior execution
/// 3. Core handler execution
/// 4. Response post-processing
/// 
/// Design principles:
/// - Single responsibility per handler
/// - Configurable behavior pipeline
/// - Thread-safe operations
/// - Cancellation support
/// </remarks>
public class DefaultNexMediator : INexMediator
{
    private readonly IServiceProvider _serviceProvider;
    private readonly NexMediatorOptions _options;
    private readonly ILogger<DefaultNexMediator>? _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultNexMediator"/> class
    /// </summary>
    /// <param name="serviceProvider">The service provider for resolving handlers and behaviors</param>
    /// <param name="logger">Optional logger for diagnostics</param>
    /// <param name="options">Configuration options for the mediator</param>
    public DefaultNexMediator(IServiceProvider serviceProvider, ILogger<DefaultNexMediator>? logger, NexMediatorOptions options)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options ?? throw new ArgumentNullException(nameof(options));

        try
        {
            _options.Validate();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "NexMediatorOptions validation failed.");
            throw;
        }
    }

    /// <summary>
    /// Sends a request through the mediator pipeline to its handler
    /// </summary>
    /// <typeparam name="TResponse">Expected response type</typeparam>
    /// <param name="request">Request to process</param>
    /// <param name="cancellationToken">Optional cancellation token</param>
    /// <returns>Handler response</returns>
    /// <remarks>
    /// Processing steps:
    /// 1. Parameter validation
    /// 2. Handler resolution
    /// 3. Behavior ordering
    /// 4. Pipeline execution
    /// 5. Response return
    /// 
    /// Behaviors execute in configured order, wrapping handler execution.
    /// </remarks>
    /// <exception cref="ArgumentNullException">When request is null</exception>
    /// <exception cref="NexMediatorException">When no handler exists</exception>
    public Task<TResponse> Send<TResponse>(INexRequest<TResponse> request, CancellationToken cancellationToken = default)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));

        return NexRequestExecutor.Dispatch(request, _serviceProvider, cancellationToken);
    }

    /// <summary>
    /// Publishes a notification to all registered handlers
    /// </summary>
    /// <typeparam name="TNotification">Notification type</typeparam>
    /// <param name="notification">Notification to publish</param>
    /// <param name="cancellationToken">Optional cancellation token</param>
    /// <returns>Task representing completion</returns>
    /// <remarks>
    /// Characteristics:
    /// - Parallel handler execution
    /// - No response required
    /// - Fault isolation
    /// - Fire-and-forget support
    /// </remarks>
    /// <exception cref="ArgumentNullException">When notification is null</exception>
    public async Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
        where TNotification : INexNotification
    {
        if (notification == null)
            throw new ArgumentNullException(nameof(notification));

        await NexNotificationExecutor.Publish(notification, _serviceProvider, cancellationToken);
    }

    /// <summary>
    /// Processes a stream request and returns an asynchronous sequence of responses.
    /// </summary>
    /// <typeparam name="TResponse">The type of each response item in the stream.</typeparam>
    /// <param name="request">The request to process.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>An asynchronous stream of <typeparamref name="TResponse"/> items.</returns>
    /// <remarks>
    /// Supports high-throughput and memory-efficient scenarios:
    /// - Processing large datasets
    /// - Real-time data feeds
    /// - Long-running or paginated operations
    /// 
    /// Features:
    /// - Supports backpressure
    /// - Responds to cancellation
    /// - Ensures proper resource cleanup
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown when the request is null.</exception>
    /// <exception cref="NexMediatorException">
    /// Thrown when no handler is found or the handler returns an invalid result.
    /// </exception>
    public IAsyncEnumerable<TResponse> Stream<TResponse>(
    INexStreamRequest<TResponse> request,
    CancellationToken cancellationToken = default)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));

        return NexStreamExecutor.Execute(request, _serviceProvider, cancellationToken);
    }
}
