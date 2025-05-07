
using Moq;
using NexMediator.Abstractions.Interfaces;

namespace NexMediator.Core.Tests.Helpers;

public static class BehaviorFactory
{
    public static INexPipelineBehavior<SampleRequest, SampleResponse> MakeBehavior(string name, List<string> callSequence)
    {
        var mock = new Mock<INexPipelineBehavior<SampleRequest, SampleResponse>>();
        mock.Setup(b => b.Handle(
                It.IsAny<SampleRequest>(),
                It.IsAny<RequestHandlerDelegate<SampleResponse>>(),
                It.IsAny<CancellationToken>()))
            .Returns((SampleRequest req, RequestHandlerDelegate<SampleResponse> next, CancellationToken token) =>
            {
                callSequence.Add(name);
                return next();
            });

        return mock.Object;
    }
}