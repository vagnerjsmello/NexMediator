using NexMediator.Abstractions.Interfaces;

namespace NexMediator.Core.Tests.Helpers;

public class ReflectionNullHandler : INexRequestHandler<CachedRequest, SampleResponse>
{
    public Task<SampleResponse> Handle(CachedRequest request, CancellationToken cancellationToken)
    {
        return null!;
    }
}