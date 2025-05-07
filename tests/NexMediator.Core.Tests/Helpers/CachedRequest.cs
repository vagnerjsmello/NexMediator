using NexMediator.Abstractions.Interfaces;

namespace NexMediator.Core.Tests.Helpers;

public class CachedRequest : ICacheableRequest<SampleResponse>
{
    public string CacheKey { get; set; } = "test-key";

    public TimeSpan? Expiration { get; set; } = TimeSpan.FromMinutes(10);

    public string? Payload { get; set; }
}