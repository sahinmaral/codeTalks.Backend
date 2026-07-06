namespace codeTalks.Application.Services.Notifications.Interfaces;

public interface IMessagePublisher
{
    Task PublishAsync<T>(T message, CancellationToken ct = default);
}