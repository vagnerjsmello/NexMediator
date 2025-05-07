using NexMediator.Abstractions.Interfaces;

namespace NexMediator.Core.Tests.Helpers;

public class SampleBehavior : INexPipelineBehavior<SampleRequest, SampleResponse>
{

    public Task<SampleResponse> Handle(SampleRequest request, RequestHandlerDelegate<SampleResponse> next, CancellationToken cancellationToken)
    {
        return next();
    }
}
