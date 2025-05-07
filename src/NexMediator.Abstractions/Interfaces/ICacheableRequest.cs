namespace NexMediator.Abstractions.Interfaces;

/// <summary>
/// Defines a request that supports caching of its response
/// </summary>
/// <typeparam name="TResponse">Type of response being cached</typeparam>
/// <remarks>
/// Caching features:
/// - Unique cache keys
/// - Configurable expiration
/// - Optional invalidation
/// - Cache miss handling
/// 
/// Implementation guidelines:
/// - Use deterministic keys
/// - Consider data freshness
/// - Handle cache failures
/// - Respect privacy concerns
/// </remarks>
public interface ICacheableRequest<TResponse> : INexRequest<TResponse>
{
    /// <summary>
    /// Gets the cache key for this request
    /// </summary>
    /// <remarks>
    /// Key requirements:
    /// - Must be unique per request
    /// - Should be deterministic
    /// - Should include version
    /// - Should be readable
    /// 
    /// Example format:
    /// "UserProfile:123:v1"
    /// </remarks>
    string CacheKey { get; }

    /// <summary>
    /// Gets the cache expiration time
    /// </summary>
    /// <remarks>
    /// Guidelines:
    /// - Null means no expiration
    /// - Use short durations
    /// - Consider data volatility
    /// - Match business needs
    /// </remarks>
    TimeSpan? Expiration { get; }
}
