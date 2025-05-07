using Microsoft.Extensions.DependencyInjection;
using NexMediator.Abstractions.Interfaces;

namespace NexMediator.Core.Pipeline;

/// <summary>
/// Internal executor that manages the complete request processing pipeline
/// </summary>
/// <typeparam name="TRequest">Request type</typeparam>
/// <typeparam name="TResponse">Response type</typeparam>
/// <remarks>
/// Pipeline sequence:
/// 1. Pre-processor execution
/// 2. Behavior pipeline execution
/// 3. Handler invocation 
/// 4. Post-processor execution
/// 
/// Features:
/// - Sequential behavior execution
/// - Resource management
/// - Error propagation
/// - Cancellation support
/// </remarks>
public class PipelineBehaviorExecutor<TRequest, TResponse>
    where TRequest : INexRequest<TResponse>
{
    private readonly INexRequestHandler<TRequest, TResponse> _handler;
    private readonly IEnumerable<INexPipelineBehavior<TRequest, TResponse>> _behaviors;
    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// Initializes a new executor instance
    /// </summary>
    /// <param name="serviceProvider">DI container access</param>
    /// <param name="handler">Core request handler</param>
    /// <param name="behaviors">Ordered behavior pipeline</param>
    /// <remarks>
    /// Dependencies:
    /// - Service provider for processors
    /// - Request handler instance
    /// - Configured behaviors
    /// </remarks>
    /// <exception cref="ArgumentNullException">When any dependency is null</exception>
    public PipelineBehaviorExecutor(
        IServiceProvider serviceProvider,
        INexRequestHandler<TRequest, TResponse> handler,
        IEnumerable<INexPipelineBehavior<TRequest, TResponse>> behaviors)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _handler = handler ?? throw new ArgumentNullException(nameof(handler));
        _behaviors = behaviors ?? Enumerable.Empty<INexPipelineBehavior<TRequest, TResponse>>();
    }

    /// <summary>
    /// Executes the complete pipeline for a request
    /// </summary>
    /// <param name="request">Request to process</param>
    /// <param name="cancellationToken">Optional cancellation token</param>
    /// <returns>Response after pipeline execution</returns>
    /// <remarks>
    /// Execution flow:
    /// 1. Check cancellation state
    /// 2. Execute pre-processors
    /// 3. Run main pipeline
    /// 4. Execute post-processors
    /// 5. Return final result
    /// </remarks>
    /// <exception cref="OperationCanceledException">When operation is cancelled</exception>
    public async Task<TResponse> Execute(TRequest request, CancellationToken cancellationToken)
    {
        // Check cancellation before starting pipeline
        cancellationToken.ThrowIfCancellationRequested();

        // Execute pre-processors
        var preProcessors = _serviceProvider.GetServices<INexRequestPreProcessor<TRequest>>();
        foreach (var processor in preProcessors)
        {
            await processor.Process(request, cancellationToken);
        }

        // Execute the main pipeline
        var response = await ExecutePipeline(request, cancellationToken);

        // Execute post-processors
        var postProcessors = _serviceProvider.GetServices<INexRequestPostProcessor<TRequest, TResponse>>();
        foreach (var processor in postProcessors)
        {
            await processor.Process(request, response, cancellationToken);
        }

        return response;
    }

    /// <summary>
    /// Executes the core behavior pipeline
    /// </summary>
    /// <param name="request">Request to process</param>
    /// <param name="cancellationToken">Optional cancellation token</param>
    /// <returns>Pipeline execution result</returns>
    /// <remarks>
    /// Implementation:
    /// - Builds nested delegates
    /// - Executes behaviors in order
    /// - Handles cancellation
    /// - Manages handler invocation
    /// </remarks>
    private Task<TResponse> ExecutePipeline(TRequest request, CancellationToken cancellationToken)
    {
        var pipeline = _behaviors.Reverse()
            .Aggregate(
                (RequestHandlerDelegate<TResponse>)(async () =>
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    return await _handler.Handle(request, cancellationToken);
                }),
                (next, pipeline) => async () =>
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    return await pipeline.Handle(request, next, cancellationToken);
                });

        return pipeline();
    }
}
