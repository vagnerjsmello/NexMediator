
using NexMediator.Abstractions.Interfaces;

namespace NexMediator.Pipeline.Tests.Helpers;

public class FakeCache : ICache
{
    private readonly List<string> _removedKeys = new();
    private readonly List<string> _removedPrefixes = new();

    public IReadOnlyList<string> RemovedKeys => _removedKeys;
    public IReadOnlyList<string> RemovedPrefixes => _removedPrefixes;

    public Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
        => Task.FromResult<T?>(default);

    public Task SetAsync<T>(string key, T value, int? ttl = null, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        _removedKeys.Add(key);
        return Task.CompletedTask;
    }

    public Task RemoveByPrefixAsync(string prefix, CancellationToken cancellationToken = default)
    {
        _removedPrefixes.Add(prefix);
        return Task.CompletedTask;
    }
}

