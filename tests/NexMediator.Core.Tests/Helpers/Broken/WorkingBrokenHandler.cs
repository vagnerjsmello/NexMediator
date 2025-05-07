using NexMediator.Abstractions.Interfaces;

namespace NexMediator.Core.Tests.Helpers.Broken;

public class WorkingBrokenHandler : INexRequestHandler<BrokenRequest, string>
{
    public Task<string> Handle(BrokenRequest request, CancellationToken cancellationToken)
    {
        return Task.FromResult("This should not be reached");
    }
}
