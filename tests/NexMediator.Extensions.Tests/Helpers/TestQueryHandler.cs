using NexMediator.Abstractions.Interfaces;

namespace NexMediator.Extensions.Tests.Helpers;

public class TestQueryHandler : INexRequestHandler<TestQuery, int>
{
    public Task<int> Handle(TestQuery request, CancellationToken ct) => Task.FromResult(request.X * 2);
}
