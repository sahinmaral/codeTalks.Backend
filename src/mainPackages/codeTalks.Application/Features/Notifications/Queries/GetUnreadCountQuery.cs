using codeTalks.Application.Services;
using codeTalks.Application.Services.Notifications.Interfaces;
using codeTalks.Application.Services.Repositories;
using codeTalks.Domain;
using Core.Application.CQRS;

namespace codeTalks.Application.Features.Notifications.Queries;

public class GetUnreadCountQuery : IRequest<int>
{
    public class GetUnreadCountQueryHandler(
        IUnreadTracker unreadTracker,
        ICurrentUserService currentUserService,
        IChannelRepository channelRepository)
        : IRequestHandler<GetUnreadCountQuery, int>
    {
        public async Task<int> Handle(GetUnreadCountQuery query, CancellationToken ct)
        {
            var userId = await currentUserService.GetCurrentUserIdAsync();

            var channels = await channelRepository.GetChannelUsersAsync(
                x => x.UserId == userId && x.Status == ChannelUserStatus.Accepted, ct);

            var counts = await Task.WhenAll(
                channels.Select(c => unreadTracker.GetCountAsync(userId, c.ChannelId, ct)));

            return (int)counts.Sum();
        }
    }
}