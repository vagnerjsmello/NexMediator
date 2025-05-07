using NexMediator.Abstractions.Interfaces;

namespace NexMediator.Core.Tests.Helpers;

public class FakeTransactionManager : ITransactionManager
{
    public Task BeginAsync() => Task.CompletedTask;

    public Task BeginTransactionAsync(CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task CommitTransactionAsync(CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
        => Task.CompletedTask;
}
