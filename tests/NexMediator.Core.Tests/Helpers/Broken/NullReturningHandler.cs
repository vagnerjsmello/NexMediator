using NexMediator.Abstractions.Interfaces;

namespace NexMediator.Core.Tests.Helpers.Broken;

public class NullReturningHandler : INexRequestHandler<BrokenRequest, string>
{
    public Task<string> Handle(BrokenRequest request, CancellationToken cancellationToken)
    {
        return null!;
    }
}
