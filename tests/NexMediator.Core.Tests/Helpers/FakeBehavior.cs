using NexMediator.Abstractions.Interfaces;

namespace NexMediator.Core.Tests.Helpers;

public class FakeBehavior<TRequest, TResponse> : INexPipelineBehavior<TRequest, TResponse>
    where TRequest : INexRequest<TResponse>
{
    public Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        return next();
    }
}

