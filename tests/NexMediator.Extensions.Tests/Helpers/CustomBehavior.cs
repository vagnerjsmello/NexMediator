using NexMediator.Abstractions.Interfaces;

namespace NexMediator.Extensions.Tests.Helpers;

public class CustomBehavior : INexPipelineBehavior<TestQuery, int>
{
    public Task<int> Handle(TestQuery request, RequestHandlerDelegate<int> next, CancellationToken cancellationToken)
        => next();
}