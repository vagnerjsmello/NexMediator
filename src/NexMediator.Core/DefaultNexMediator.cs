using NexMediator.Abstractions.Interfaces;
using NexMediator.Core.Internal;

namespace NexMediator.Core;

/// <summary>
/// Default implementation of INexMediator.
/// Routes requests, notifications, and streams through a cached pipeline delegate.
/// </summary>
public class DefaultNexMediator : INexMediator
{
    private readonly IServiceProvider _provider;
    private readonly NexMediatorOptions _options;

    /// <summary>
    /// Initializes a new DefaultNexMediator instance.
    /// Validates pipeline options at startup and throws if invalid.
    /// </summary>
    /// <param name="provider">
    /// Service provider for resolving handlers, behaviors, and scoped services.
    /// </param>
    /// <param name="options">
    /// Configuration options for the mediator pipeline.
    /// </param>
    public DefaultNexMediator(IServiceProvider provider, NexMediatorOptions options)
    {
        _provider = provider ?? throw new ArgumentNullException(nameof(provider));
        _options = options ?? throw new ArgumentNullException(nameof(options));

        var warnings = _options.Validate();
        if (warnings.Count > 0)
            throw new InvalidOperationException($"Pipeline configuration validation failed: {string.Join("; ", warnings)}");
    }

    /// <summary>
    /// Sends a request through the compiled, cached pipeline and returns its response.
    /// </summary>
    /// <typeparam name="TResponse">Type of the response.</typeparam>
    /// <param name="request">The request to send.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The response from the handler.</returns>
    public Task<TResponse> Send<TResponse>(INexRequest<TResponse> request, CancellationToken cancellationToken = default)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));

        return NexRequestExecutor.Dispatch(request, _provider, cancellationToken);
    }

    /// <summary>
    /// Publishes a notification through the pipeline without caching.
    /// </summary>
    /// <typeparam name="TNotification">Type of the notification.</typeparam>
    /// <param name="notification">The notification to publish.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A task representing the publish operation.</returns>
    public Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
        where TNotification : INexNotification
    {
        if (notification == null)
            throw new ArgumentNullException(nameof(notification));

        return NexNotificationExecutor.Publish(notification, _provider, cancellationToken);
    }

    /// <summary>
    /// Streams responses for a stream request through the pipeline without caching.
    /// </summary>
    /// <typeparam name="TResponse">Type of each streamed response.</typeparam>
    /// <param name="request">The stream request.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>An async enumerable of responses.</returns>
    public IAsyncEnumerable<TResponse> Stream<TResponse>(INexStreamRequest<TResponse> request, CancellationToken cancellationToken = default)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));

        return NexStreamExecutor.Execute(request, _provider, cancellationToken);
    }
}
