using NexMediator.Abstractions.Interfaces;

namespace NexMediator.Core.Tests.Helpers;

public class FakeBehaviorA<TRequest, TResponse> : INexPipelineBehavior<TRequest, TResponse>
    where TRequest : INexRequest<TResponse>
{
    public Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    => next();
}
