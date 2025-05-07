namespace NexMediator.Abstractions.Interfaces;

/// <summary>
/// Defines the contract for caching operations in the mediator pipeline
/// </summary>
/// <remarks>
/// Core responsibilities:
/// - Provide thread-safe operations
/// - Handle serialization/deserialization
/// - Manage cache expiration
/// - Ensure proper error handling
/// </remarks>
public interface ICache
{
    /// <summary>
    /// Retrieves a value from the cache
    /// </summary>
    /// <typeparam name="T">The type to retrieve</typeparam>
    /// <param name="key">The cache key</param>
    /// <param name="cancellationToken">Optional cancellation token</param>
    /// <returns>The cached value if found, otherwise null</returns>
    /// <remarks>
    /// Implementation notes:
    /// - Return null for missing keys
    /// - Handle type conversion safely
    /// - Check expiration before returning
    /// </remarks>
    Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Stores a value in the cache
    /// </summary>
    /// <typeparam name="T">The type to cache</typeparam>
    /// <param name="key">The cache key</param>
    /// <param name="value">The value to cache</param>
    /// <param name="ttl">Time to live in seconds (null for no expiration)</param>
    /// <param name="cancellationToken">Optional cancellation token</param>
    /// <remarks>
    /// Implementation notes:
    /// - Handle complex type serialization
    /// - Support concurrent updates
    /// - Validate inputs
    /// </remarks>
    Task SetAsync<T>(string key, T value, int? ttl = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a value from the cache
    /// </summary>
    /// <param name="key">The cache key</param>
    /// <param name="cancellationToken">Optional cancellation token</param>
    /// <remarks>
    /// Implementation notes:
    /// - Handle missing keys gracefully
    /// - Ensure atomic operations
    /// - Clean up all related metadata
    /// </remarks>
    Task RemoveAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes all cache entries whose keys begin with the specified prefix.
    /// </summary>
    /// <param name="prefix">The prefix used to identify which cache entries to remove.</param>
    /// <param name="cancellationToken">A token to observe while waiting for the operation to complete.</param>
    /// <returns>A task representing the asynchronous removal of matching cache entries.</returns>
    Task RemoveByPrefixAsync(string prefix, CancellationToken cancellationToken = default);
}
