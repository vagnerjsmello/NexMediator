using Microsoft.Extensions.Logging;
using NexMediator.Abstractions.Interfaces;

namespace NexMediator.Pipeline.Behaviors;

/// <summary>
/// Pipeline behavior that handles transactions for commands only.
/// </summary>
/// <typeparam name="TRequest">The request type.</typeparam>
/// <typeparam name="TResponse">The response type.</typeparam>
public class TransactionBehavior<TRequest, TResponse> : INexPipelineBehavior<TRequest, TResponse>
    where TRequest : INexRequest<TResponse>
{
    private readonly ITransactionManager _transactionManager;
    private readonly ILogger<TransactionBehavior<TRequest, TResponse>> _logger;

    public TransactionBehavior(
        ITransactionManager transactionManager,
        ILogger<TransactionBehavior<TRequest, TResponse>> logger)
    {
        _transactionManager = transactionManager ?? throw new ArgumentNullException(nameof(transactionManager));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {

        if (request is not INexCommand<TResponse>)
        {
            _logger.LogDebug("Skipping transaction for non-command request {RequestType}", typeof(TRequest).Name);
            return await next();
        }

        await _transactionManager.BeginTransactionAsync(cancellationToken);

        try
        {
            _logger.LogDebug("Started transaction for command {RequestType}", typeof(TRequest).Name);

            var response = await next();

            await _transactionManager.CommitTransactionAsync(cancellationToken);

            _logger.LogDebug("Committed transaction for command {RequestType}", typeof(TRequest).Name);

            return response;
        }
        catch (Exception ex)
        {
            await _transactionManager.RollbackTransactionAsync(cancellationToken);

            _logger.LogWarning(ex, "Rolled back transaction for command {RequestType} due to error", typeof(TRequest).Name);

            throw;
        }
    }
}
