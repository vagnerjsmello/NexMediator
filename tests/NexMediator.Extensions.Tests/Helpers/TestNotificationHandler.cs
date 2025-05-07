using NexMediator.Abstractions.Interfaces;

namespace NexMediator.Extensions.Tests.Helpers;

public class TestNotificationHandler : INexNotificationHandler<TestNotification>
{
    public Task Handle(TestNotification notification, CancellationToken ct) => Task.CompletedTask;
}
