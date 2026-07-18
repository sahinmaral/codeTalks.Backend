using System.Text;
using Core.Application.CQRS;
using Core.Application.Pipelines.Caching;
using FluentAssertions;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Core.Application.UnitTests.Pipelines;

// CachingBehavior: bypass -> straight to next; cache hit -> return the deserialized value
// without calling next; cache miss -> call next, then store the serialized response.
public class CachingBehaviorTests
{
    private const string CacheKey = "test-key";

    private readonly IDistributedCache _cache = Substitute.For<IDistributedCache>();
    private readonly ILogger<CachingBehavior<CachableRequest, TestResponse>> _logger =
        Substitute.For<ILogger<CachingBehavior<CachableRequest, TestResponse>>>();
    private readonly IConfiguration _configuration = new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?> { ["CacheSettings:SlidingExpiration"] = "2" })
        .Build();

    private CachingBehavior<CachableRequest, TestResponse> CreateBehavior() => new(_cache, _logger, _configuration);

    [Fact]
    public async Task Handle_WhenBypassCache_CallsNextAndSkipsCache()
    {
        var behavior = CreateBehavior();
        var response = new TestResponse { Value = "fresh" };
        var nextCalls = 0;
        RequestHandlerDelegate<TestResponse> next = () => { nextCalls++; return Task.FromResult(response); };

        var result = await behavior.Handle(new CachableRequest { BypassCache = true }, next, CancellationToken.None);

        result.Should().BeSameAs(response);
        nextCalls.Should().Be(1);
        await _cache.DidNotReceive().GetAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenCacheHit_ReturnsCachedValueWithoutCallingNext()
    {
        var behavior = CreateBehavior();
        _cache.GetAsync(CacheKey, Arg.Any<CancellationToken>())
            .Returns(Encoding.UTF8.GetBytes("{\"Value\":\"cached\"}"));
        var nextCalls = 0;
        RequestHandlerDelegate<TestResponse> next = () => { nextCalls++; return Task.FromResult(new TestResponse { Value = "fresh" }); };

        var result = await behavior.Handle(new CachableRequest(), next, CancellationToken.None);

        result.Value.Should().Be("cached");
        nextCalls.Should().Be(0);
        await _cache.DidNotReceive().SetAsync(Arg.Any<string>(), Arg.Any<byte[]>(),
            Arg.Any<DistributedCacheEntryOptions>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenCacheMiss_CallsNextAndStoresResponse()
    {
        var behavior = CreateBehavior();
        _cache.GetAsync(CacheKey, Arg.Any<CancellationToken>()).Returns((byte[]?)null);
        var response = new TestResponse { Value = "fresh" };
        var nextCalls = 0;
        RequestHandlerDelegate<TestResponse> next = () => { nextCalls++; return Task.FromResult(response); };

        var result = await behavior.Handle(new CachableRequest(), next, CancellationToken.None);

        result.Should().BeSameAs(response);
        nextCalls.Should().Be(1);
        await _cache.Received(1).SetAsync(CacheKey, Arg.Any<byte[]>(),
            Arg.Any<DistributedCacheEntryOptions>(), Arg.Any<CancellationToken>());
    }
}