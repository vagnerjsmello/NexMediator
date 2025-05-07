using NexMediator.Abstractions.Interfaces;
using NexMediator.Core.Tests.Helpers.Broken;

namespace NexMediator.Core.Tests.Helpers;

public class FakeInvalidStreamHandler : INexStreamRequestHandler<BrokenStreamRequest, string>
{
    public IAsyncEnumerable<string> Handle(BrokenStreamRequest request, CancellationToken cancellationToken)
    {
        return null!;
    }
}

