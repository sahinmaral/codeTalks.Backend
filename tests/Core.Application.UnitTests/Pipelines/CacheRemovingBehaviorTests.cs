using Core.Application.CQRS;
using Core.Application.Pipelines.Caching;
using FluentAssertions;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Core.Application.UnitTests.Pipelines;

// CacheRemovingBehavior calls next, then evicts the cache key — unless the request bypasses cache.
public class CacheRemovingBehaviorTests
{
    private const string CacheKey = "test-key";

    private readonly IDistributedCache _cache = Substitute.For<IDistributedCache>();
    private readonly ILogger<CacheRemovingBehavior<CacheRemoverRequest, TestResponse>> _logger =
        Substitute.For<ILogger<CacheRemovingBehavior<CacheRemoverRequest, TestResponse>>>();

    private CacheRemovingBehavior<CacheRemoverRequest, TestResponse> CreateBehavior() => new(_cache, _logger);

    [Fact]
    public async Task Handle_WhenBypassCache_CallsNextAndDoesNotRemove()
    {
        var behavior = CreateBehavior();
        var response = new TestResponse();
        var nextCalls = 0;
        RequestHandlerDelegate<TestResponse> next = () => { nextCalls++; return Task.FromResult(response); };

        var result = await behavior.Handle(new CacheRemoverRequest { BypassCache = true }, next, CancellationToken.None);

        result.Should().BeSameAs(response);
        nextCalls.Should().Be(1);
        await _cache.DidNotReceive().RemoveAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenNotBypassed_CallsNextThenRemovesKey()
    {
        var behavior = CreateBehavior();
        var response = new TestResponse();
        var nextCalls = 0;
        RequestHandlerDelegate<TestResponse> next = () => { nextCalls++; return Task.FromResult(response); };

        var result = await behavior.Handle(new CacheRemoverRequest(), next, CancellationToken.None);

        result.Should().BeSameAs(response);
        nextCalls.Should().Be(1);
        await _cache.Received(1).RemoveAsync(CacheKey, Arg.Any<CancellationToken>());
    }
}