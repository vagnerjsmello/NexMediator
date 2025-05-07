using Microsoft.Extensions.Logging;
using NexMediator.Abstractions.Context;
using NexMediator.Abstractions.Interfaces;
using System.Diagnostics;

namespace NexMediator.Pipeline.Behaviors;

/// <summary>
/// Pipeline behavior that logs requests and responses,
/// optionally including a correlation ID if available.
/// </summary>
/// <typeparam name="TRequest">The request type.</typeparam>
/// <typeparam name="TResponse">The response type.</typeparam>
public class LoggingBehavior<TRequest, TResponse> : INexPipelineBehavior<TRequest, TResponse>
    where TRequest : INexRequest<TResponse>
{
    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;
    private readonly ICorrelationContext? _correlation;

    /// <summary>
    /// Creates an instance of <see cref="LoggingBehavior{TRequest,TResponse}"/>
    /// without correlation support.
    /// </summary>
    /// <param name="logger">The logger instance from DI.</param>
    public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _correlation = null;
    }

    /// <summary>
    /// Creates an instance of <see cref="LoggingBehavior{TRequest,TResponse}"/>
    /// with correlation support.
    /// </summary>
    /// <param name="logger">The logger instance from DI.</param>
    /// <param name="correlation">The correlation context providing an ID.</param>
    public LoggingBehavior(
        ILogger<LoggingBehavior<TRequest, TResponse>> logger, ICorrelationContext correlation) : this(logger)
    {
        _correlation = correlation ?? throw new ArgumentNullException(nameof(correlation));
    }

    /// <inheritdoc/>
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        var correlationId = _correlation?.CorrelationId;
        var stopwatch = Stopwatch.StartNew();

        // Log start of request
        if (!string.IsNullOrEmpty(correlationId))
        {
            _logger.LogInformation(
                "Handling {RequestType} started at {StartTime} (CorrelationId: {CorrelationId})",
                requestName,
                DateTime.UtcNow,
                correlationId);
        }
        else
        {
            _logger.LogInformation(
                "Handling {RequestType} started at {StartTime}",
                requestName,
                DateTime.UtcNow);
        }

        try
        {
            var response = await next();
            stopwatch.Stop();

            // Log successful completion
            if (!string.IsNullOrEmpty(correlationId))
            {
                _logger.LogInformation(
                    "Handled {RequestType} completed in {ElapsedMilliseconds}ms (CorrelationId: {CorrelationId}) with response {@Response}",
                    requestName,
                    stopwatch.ElapsedMilliseconds,
                    correlationId,
                    response);
            }
            else
            {
                _logger.LogInformation(
                    "Handled {RequestType} completed in {ElapsedMilliseconds}ms with response {@Response}",
                    requestName,
                    stopwatch.ElapsedMilliseconds,
                    response);
            }

            return response;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            // Log error
            if (!string.IsNullOrEmpty(correlationId))
            {
                _logger.LogError(
                    ex,
                    "Error handling {RequestType} after {ElapsedMilliseconds}ms (CorrelationId: {CorrelationId}) {@Request}",
                    requestName,
                    stopwatch.ElapsedMilliseconds,
                    correlationId,
                    request);
            }
            else
            {
                _logger.LogError(
                    ex,
                    "Error handling {RequestType} after {ElapsedMilliseconds}ms {@Request}",
                    requestName,
                    stopwatch.ElapsedMilliseconds,
                    request);
            }

            throw;
        }
    }
}
