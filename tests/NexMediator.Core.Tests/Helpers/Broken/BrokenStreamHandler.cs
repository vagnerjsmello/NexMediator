namespace NexMediator.Core.Tests.Helpers.Broken;

public class BrokenStreamHandler
{
    public Task<string> Handle(BrokenStreamRequest request, CancellationToken cancellationToken)
    {
        return Task.FromResult("Not a stream");
    }
}
