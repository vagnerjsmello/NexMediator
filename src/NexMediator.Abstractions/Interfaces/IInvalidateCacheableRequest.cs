namespace NexMediator.Abstractions.Interfaces;

/// <summary>
/// Requests that certain cache entries be invalidated when processed.
/// </summary>
public interface IInvalidateCacheableRequest
{
    /// <summary>
    /// Specific cache keys to remove.
    /// </summary>
    IReadOnlyCollection<string> KeysToInvalidate { get; }

    /// <summary>
    /// Prefixes for cache keys to remove in bulk.
    /// </summary>
    IReadOnlyCollection<string> PrefixesToInvalidate { get; }
}
