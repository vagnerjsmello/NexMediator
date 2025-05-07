using Microsoft.Extensions.Logging;
using NexMediator.Abstractions.Exceptions;
using NexMediator.Abstractions.Interfaces;

namespace NexMediator.Pipeline.Behaviors;
/// <summary>
/// A pipeline behavior that adds caching to supported requests.
/// Also supports cache invalidation for requests implementing <see cref="IInvalidateCacheableRequest"/>.
/// </summary>
/// <typeparam name="TRequest">The type of the request.</typeparam>
/// <typeparam name="TResponse">The type of the response.</typeparam>
public class CachingBehavior<TRequest, TResponse> : INexPipelineBehavior<TRequest, TResponse>
    where TRequest : INexRequest<TResponse>
{
    private readonly ICache _cache;
    private readonly ILogger<CachingBehavior<TRequest, TResponse>> _logger;

    /// <summary>
    /// Creates a new instance of <see cref="CachingBehavior{TRequest, TResponse}"/>.
    /// </summary>
    /// <param name="cache">The cache service.</param>
    /// <param name="logger">The logger instance.</param>
    public CachingBehavior(ICache cache, ILogger<CachingBehavior<TRequest, TResponse>> logger)
    {
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        //Invalidate cache for requests that require it
        if (request is IInvalidateCacheableRequest inv)
        {
            foreach (var key in inv.KeysToInvalidate)
                await _cache.RemoveAsync(key, cancellationToken);
            foreach (var prefix in inv.PrefixesToInvalidate)
                await _cache.RemoveByPrefixAsync(prefix, cancellationToken);
        }

        //Proceed with caching logic
        if (request is not ICacheableRequest<TResponse> cacheable)
        {
            return await next();
        }

        var cacheKey = cacheable.CacheKey;
        TResponse? cached = default;

        try
        {
            cached = await _cache.GetAsync<TResponse>(cacheKey, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Cache read failed for key {CacheKey}", cacheKey);
        }

        if (cached != null)
        {
            _logger.LogDebug("Cache hit for {RequestType} [Key: {CacheKey}]", typeof(TRequest).Name, cacheKey);
            return cached;
        }

        _logger.LogDebug("Cache miss for {RequestType} [Key: {CacheKey}]", typeof(TRequest).Name, cacheKey);

        var response = await next();

        if (response == null)
        {
            throw new NexMediatorException($"Handler returned null for request type {typeof(TRequest).Name}");
        }

        int? ttl = cacheable.Expiration.HasValue
            ? (int)cacheable.Expiration.Value.TotalSeconds
            : null;

        try
        {
            await _cache.SetAsync(cacheKey, response, ttl, cancellationToken);
            _logger.LogDebug(
                "Response cached for {RequestType} [Key: {CacheKey}, TTL: {TTL}s]",
                typeof(TRequest).Name, cacheKey, ttl);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Cache write failed for key {CacheKey}", cacheKey);
        }

        return response;
    }
}
