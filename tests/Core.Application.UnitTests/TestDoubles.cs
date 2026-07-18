using Core.Application.CQRS;
using Core.Application.Pipelines.Caching;

namespace Core.Application.UnitTests;

// Minimal request/response types used to instantiate the generic pipeline behaviors.
public class TestResponse
{
    public string? Value { get; set; }
}

public class ValidatableRequest : IRequest<TestResponse>
{
    public string? Name { get; set; }
}

public class CachableRequest : IRequest<TestResponse>, ICachableRequest
{
    public bool BypassCache { get; set; }
    public string CacheKey { get; set; } = "test-key";
    public TimeSpan? SlidingExpiration { get; set; }
}

public class CacheRemoverRequest : IRequest<TestResponse>, ICacheRemoverRequest
{
    public bool BypassCache { get; set; }
    public string CacheKey { get; set; } = "test-key";
}