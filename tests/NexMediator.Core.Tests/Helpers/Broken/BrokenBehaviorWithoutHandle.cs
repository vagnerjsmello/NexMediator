using NexMediator.Abstractions.Interfaces;

namespace NexMediator.Core.Tests.Helpers.Broken;

public class BrokenBehaviorWithoutHandle : INexPipelineBehavior<CachedRequest, SampleResponse>
{
    public Task<SampleResponse> Handle(CachedRequest request, RequestHandlerDelegate<SampleResponse> next, CancellationToken cancellationToken)
    {
        return Task.FromResult<SampleResponse>(null!);
    }
}

