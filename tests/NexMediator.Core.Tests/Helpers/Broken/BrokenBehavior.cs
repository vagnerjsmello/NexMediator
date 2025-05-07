using NexMediator.Abstractions.Interfaces;

namespace NexMediator.Core.Tests.Helpers.Broken;

public class BrokenBehavior<TRequest, TResponse> : INexPipelineBehavior<TRequest, TResponse>
    where TRequest : INexRequest<TResponse>
{
    public Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)

    {
        return null!;
    }
}
