using codeTalks.Application.Services.Notifications.Interfaces;

namespace codeTalks.WebAPI.IntegrationTests.Infrastructure;

/// <summary>
/// Test double for <see cref="IMessagePublisher"/>. The real implementation opens a
/// RabbitMQ connection in its constructor; integration tests run without a broker, so
/// publishing is a no-op here. Swap for a recording fake if a test needs to assert on
/// what was published.
/// </summary>
public sealed class NoOpMessagePublisher : IMessagePublisher
{
    public Task PublishAsync<T>(T message, CancellationToken ct = default) => Task.CompletedTask;
}