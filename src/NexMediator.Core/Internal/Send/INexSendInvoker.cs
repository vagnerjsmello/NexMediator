namespace NexMediator.Core.Internal.Send;


/// <summary>
/// Defines a contract for invoking a request pipeline with a specific request and response type.
/// </summary>
internal interface INexSendInvoker
{
    /// <summary>
    /// Executes the pipeline for the given request using the provided service provider and cancellation token.
    /// </summary>
    /// <param name="request">The request object to execute.</param>
    /// <param name="provider">The root service provider to resolve dependencies.</param>
    /// <param name="cancellationToken">A token to cancel the pipeline execution.</param>
    /// <returns>A task representing the result of the pipeline execution.</returns>
    Task<object> Invoke(object request, IServiceProvider provider, CancellationToken cancellationToken);
}

