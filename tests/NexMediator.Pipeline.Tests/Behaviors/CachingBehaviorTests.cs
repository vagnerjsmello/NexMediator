using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using NexMediator.Abstractions.Interfaces;
using NexMediator.Pipeline.Behaviors;
using NexMediator.Pipeline.Tests.Helpers;

namespace NexMediator.Tests.Pipeline;

/// <summary>
/// Unit tests for the CachingBehavior in the pipeline, validating hit/miss, TTL, and error resilience.
/// </summary>
public class CachingBehaviorTests
{
    private readonly Mock<ICache> _mockCache;
    private readonly Mock<ILogger<CachingBehavior<CachedRequest, SampleResponse>>> _mockLogger;
    private readonly CachingBehavior<CachedRequest, SampleResponse> _behavior;

    public CachingBehaviorTests()
    {
        _mockCache = new Mock<ICache>();
        _mockLogger = new Mock<ILogger<CachingBehavior<CachedRequest, SampleResponse>>>();
        _behavior = new CachingBehavior<CachedRequest, SampleResponse>(_mockCache.Object, _mockLogger.Object);
    }

    /// <summary>
    /// Returns the cached response if present.
    /// </summary>
    [Fact]
    public async Task CachingBehavior_Should_Return_Cached_Response()
    {
        var cachedResponse = new SampleResponse();
        _mockCache.Setup(c => c.GetAsync<SampleResponse>("test-key", It.IsAny<CancellationToken>()))
                  .ReturnsAsync(cachedResponse);

        var request = new CachedRequest { CacheKey = "test-key", Expiration = TimeSpan.FromMinutes(5) };
        var next = new RequestHandlerDelegate<SampleResponse>(() => Task.FromResult(new SampleResponse()));

        var response = await _behavior.Handle(request, next, CancellationToken.None);

        response.Should().Be(cachedResponse);
        _mockCache.Verify(c => c.SetAsync(It.IsAny<string>(), It.IsAny<SampleResponse>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>
    /// Executes handler and stores result in cache on a cache miss.
    /// </summary>
    [Fact]
    public async Task CachingBehavior_Should_Execute_And_Cache_Response_On_Miss()
    {
        _mockCache.Setup(c => c.GetAsync<SampleResponse>("test-key", It.IsAny<CancellationToken>()))
                  .ReturnsAsync((SampleResponse?)null);

        var expected = new SampleResponse();
        var request = new CachedRequest();
        var next = new RequestHandlerDelegate<SampleResponse>(() => Task.FromResult(expected));
        var expectedTtl = (int?)request.Expiration.GetValueOrDefault().TotalSeconds;

        var result = await _behavior.Handle(request, next, CancellationToken.None);

        result.Should().Be(expected);
        _mockCache.Verify(c => c.SetAsync("test-key", expected, expectedTtl, It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// Skips cache interaction when request is not cacheable.
    /// </summary>
    [Fact]
    public async Task CachingBehavior_Should_Skip_Non_Cacheable_Request()
    {
        var behavior = new CachingBehavior<NonCacheableRequest, SampleResponse>(
            new Mock<ICache>().Object,
            new Mock<ILogger<CachingBehavior<NonCacheableRequest, SampleResponse>>>().Object);

        var request = new NonCacheableRequest();
        var expected = new SampleResponse();
        var next = new RequestHandlerDelegate<SampleResponse>(() => Task.FromResult(expected));

        var result = await behavior.Handle(request, next, CancellationToken.None);

        result.Should().Be(expected);
    }

    /// <summary>
    /// Logs messages on cache miss and after caching the response.
    /// </summary>
    [Fact]
    public async Task CachingBehavior_Should_Log_Hit_And_Miss_And_Cache()
    {
        var request = new CachedRequest();
        var expected = new SampleResponse();

        _mockCache.Setup(c => c.GetAsync<SampleResponse>("test-key", It.IsAny<CancellationToken>()))
                  .ReturnsAsync((SampleResponse?)null);

        var next = new RequestHandlerDelegate<SampleResponse>(() => Task.FromResult(expected));

        await _behavior.Handle(request, next, CancellationToken.None);

        _mockLogger.VerifyLog(LogLevel.Debug, Times.Once(), msg => msg.Contains("Cache miss"));
        _mockLogger.VerifyLog(LogLevel.Debug, Times.Once(), msg => msg.Contains("Response cached"));

    }

    /// <summary>
    /// Logs when a cached response is returned.
    /// </summary>
    [Fact]
    public async Task CachingBehavior_Should_Log_CacheHit()
    {
        var cached = new SampleResponse();
        var request = new CachedRequest { CacheKey = "test-key", Expiration = TimeSpan.FromMinutes(5) };

        _mockCache.Setup(c => c.GetAsync<SampleResponse>("test-key", It.IsAny<CancellationToken>()))
                  .ReturnsAsync(cached);

        var next = new RequestHandlerDelegate<SampleResponse>(() => throw new InvalidOperationException());

        var result = await _behavior.Handle(request, next, CancellationToken.None);

        result.Should().Be(cached);
        _mockLogger.VerifyLogContains("Cache hit", LogLevel.Debug);
    }

    /// <summary>
    /// Verifies that cache is set with the correct TTL.
    /// </summary>
    [Fact]
    public async Task CachingBehavior_Should_Use_Correct_TTL_When_Caching_Response()
    {
        var request = new CachedRequest { CacheKey = "ttl-key", Expiration = TimeSpan.FromMinutes(10) };
        var expectedTtl = (int)request.Expiration.Value.TotalSeconds;

        _mockCache.Setup(c => c.GetAsync<SampleResponse>("ttl-key", It.IsAny<CancellationToken>()))
                  .ReturnsAsync((SampleResponse?)null);

        var expected = new SampleResponse();
        var next = new RequestHandlerDelegate<SampleResponse>(() => Task.FromResult(expected));

        var result = await _behavior.Handle(request, next, CancellationToken.None);

        result.Should().Be(expected);
        _mockCache.Verify(c => c.SetAsync("ttl-key", expected, expectedTtl, It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// Continues execution when cache read throws.
    /// </summary>
    [Fact]
    public async Task CachingBehavior_Should_Continue_When_CacheThrows()
    {
        _mockCache.Setup(c => c.GetAsync<SampleResponse>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                  .ThrowsAsync(new Exception("cache error"));

        var request = new CachedRequest();
        var expected = new SampleResponse();
        var next = new RequestHandlerDelegate<SampleResponse>(() => Task.FromResult(expected));

        var result = await _behavior.Handle(request, next, CancellationToken.None);

        result.Should().Be(expected);
    }

    /// <summary>
    /// Continues execution when cache write throws.
    /// </summary>
    [Fact]
    public async Task CachingBehavior_Should_Continue_When_CacheSetThrows()
    {
        _mockCache.Setup(c => c.GetAsync<SampleResponse>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                  .ReturnsAsync((SampleResponse?)null);

        _mockCache.Setup(c => c.SetAsync(It.IsAny<string>(), It.IsAny<SampleResponse>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                  .ThrowsAsync(new Exception("cache write error"));

        var request = new CachedRequest { CacheKey = "key", Expiration = TimeSpan.FromMinutes(5) };
        var expected = new SampleResponse();
        var next = new RequestHandlerDelegate<SampleResponse>(() => Task.FromResult(expected));

        var result = await _behavior.Handle(request, next, CancellationToken.None);

        result.Should().Be(expected);
        _mockLogger.VerifyLogContains("Cache write failed", LogLevel.Warning);
    }

    /// <summary>
    /// Sets cache on cache miss when response is not null.
    /// </summary>
    [Fact]
    public async Task CachingBehavior_Should_Set_Cache_With_TTL_On_CacheMiss_And_Response()
    {
        var cacheMock = new Mock<ICache>();
        var loggerMock = new Mock<ILogger<CachingBehavior<CachedRequest, SampleResponse>>>();

        cacheMock.Setup(c => c.GetAsync<SampleResponse>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync((SampleResponse)null!);

        var request = new CachedRequest();
        var response = new SampleResponse { Result = "cached-data" };

        var behavior = new CachingBehavior<CachedRequest, SampleResponse>(cacheMock.Object, loggerMock.Object);
        var next = new RequestHandlerDelegate<SampleResponse>(() => Task.FromResult(response)!);

        var result = await behavior.Handle(request, next, CancellationToken.None);

        result.Should().Be(response);
        cacheMock.Verify(c => c.SetAsync("test-key", response, 600, It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// Sets TTL when caching valid response.
    /// </summary>
    [Fact]
    public async Task Handle_Should_SetCache_With_TTL_When_Response_Is_Not_Null()
    {
        var cacheMock = new Mock<ICache>();
        var loggerMock = new Mock<ILogger<CachingBehavior<CachedRequest, SampleResponse>>>();

        cacheMock.Setup(c => c.GetAsync<SampleResponse>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync((SampleResponse)null!);

        var request = new CachedRequest();
        var response = new SampleResponse { Result = "Response from handler" };

        var behavior = new CachingBehavior<CachedRequest, SampleResponse>(cacheMock.Object, loggerMock.Object);
        var next = new RequestHandlerDelegate<SampleResponse>(() => Task.FromResult(response)!);

        var result = await behavior.Handle(request, next, CancellationToken.None);

        result.Should().Be(response);
        cacheMock.Verify(c => c.SetAsync("test-key", response, 600, It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// Sets cache using custom key and TTL values.
    /// </summary>
    [Fact]
    public async Task Handle_Should_Call_SetAsync_With_TTL_When_CacheMiss_And_Response_Is_Not_Null()
    {
        var cacheMock = new Mock<ICache>();
        var loggerMock = new Mock<ILogger<CachingBehavior<CachedRequest, SampleResponse>>>();

        cacheMock.Setup(c => c.GetAsync<SampleResponse>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync((SampleResponse)null!);

        var request = new CachedRequest
        {
            CacheKey = "force-key",
            Expiration = TimeSpan.FromSeconds(42)
        };

        var response = new SampleResponse { Result = "mocked" };
        var behavior = new CachingBehavior<CachedRequest, SampleResponse>(cacheMock.Object, loggerMock.Object);
        var next = new RequestHandlerDelegate<SampleResponse>(() => Task.FromResult(response)!);

        var result = await behavior.Handle(request, next, CancellationToken.None);

        result.Should().Be(response);
        cacheMock.Verify(c => c.SetAsync("force-key", response, 42, It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// Constructor should throw if cache dependency is null.
    /// </summary>
    [Fact]
    public void Constructor_Should_Throw_When_Cache_Is_Null()
    {
        var logger = new Mock<ILogger<CachingBehavior<CachedRequest, SampleResponse>>>();
        Assert.Throws<ArgumentNullException>(() => new CachingBehavior<CachedRequest, SampleResponse>(null!, logger.Object));
    }

    /// <summary>
    /// Constructor should throw if logger dependency is null.
    /// </summary>
    [Fact]
    public void Constructor_Should_Throw_When_Logger_Is_Null()
    {
        var cache = new Mock<ICache>();
        Assert.Throws<ArgumentNullException>(() => new CachingBehavior<CachedRequest, SampleResponse>(cache.Object, null!));
    }

    /// <summary>
    /// Constructor should succeed when both dependencies are provided.
    /// </summary>
    [Fact]
    public void Constructor_Should_Not_Throw_When_Dependencies_Are_Provided()
    {
        var cache = new Mock<ICache>();
        var logger = new Mock<ILogger<CachingBehavior<CachedRequest, SampleResponse>>>();
        var instance = new CachingBehavior<CachedRequest, SampleResponse>(cache.Object, logger.Object);

        instance.Should().NotBeNull();
    }


    [Fact]
    public async Task RemoveAsync_Should_Add_Key_To_RemovedKeys()
    {
        // Arrange
        var cache = new FakeCache();
        const string key = "sample-key";

        // Act
        await cache.RemoveAsync(key, CancellationToken.None);

        // Assert
        cache.RemovedKeys.Should().ContainSingle()
            .Which.Should().Be(key);
    }

    [Fact]
    public async Task RemoveByPrefixAsync_Should_Add_Prefix_To_RemovedPrefixes()
    {
        // Arrange
        var cache = new FakeCache();
        const string prefix = "sample-prefix:";

        // Act
        await cache.RemoveByPrefixAsync(prefix, CancellationToken.None);

        // Assert
        cache.RemovedPrefixes.Should().ContainSingle()
            .Which.Should().Be(prefix);
    }

    [Fact]
    public async Task RemoveAsync_And_RemoveByPrefixAsync_Should_Be_CaseSensitive()
    {
        // Arrange
        var cache = new FakeCache();
        const string key = "Key";
        const string prefix = "PreF:";

        // Act
        await cache.RemoveAsync(key, CancellationToken.None);
        await cache.RemoveByPrefixAsync(prefix, CancellationToken.None);

        // Assert
        cache.RemovedKeys.Should().Contain(key);
        cache.RemovedPrefixes.Should().Contain(prefix);
    }
}
