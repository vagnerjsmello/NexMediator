namespace NexMediator.Abstractions.Interfaces;

/// <summary>
/// Defines a request whose handling will be executed within a transactional scope.
/// </summary>
/// <typeparam name="TResponse">Type of the response produced by this request.</typeparam>
/// <remarks>
/// Transactional features:
/// - Begins a transaction before the handler executes  
/// - Commits the transaction if the handler completes successfully  
/// - Rolls back the transaction if an exception is thrown  
/// - Ensures atomicity across multiple data operations  
/// 
/// Implementation guidelines:
/// - Apply only to commands that modify state  
/// - Keep handlers focused on business logic and let the pipeline manage SaveChanges  
/// - Ensure error paths re-throw or bubble up exceptions to trigger rollback  
/// - Avoid long-running work inside a single transaction to reduce lock contention  
/// </remarks>
public interface ITransactionalRequest<TResponse> : INexRequest<TResponse>
{
}


