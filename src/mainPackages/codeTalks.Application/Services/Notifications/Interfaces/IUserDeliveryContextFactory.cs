namespace codeTalks.Application.Services.Notifications;

public interface IUserDeliveryContextFactory
{
    Task<UserDeliveryContext> CreateAsync(
        string userId,
        string channelId,
        CancellationToken ct = default);
}