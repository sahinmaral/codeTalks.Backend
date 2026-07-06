using codeTalks.Application.Services;
using codeTalks.Application.Services.Notifications.Interfaces;
using codeTalks.Application.Services.Repositories;
using codeTalks.Domain;
using Core.Application.CQRS;

namespace codeTalks.Application.Features.Notifications.Queries;

public class GetChannelUnreadCountQuery : IRequest<int>
{
    public string ChannelId { get; set; }
    
    public class GetChannelUnreadCountQueryHandler(
        IUnreadTracker unreadTracker,
        ICurrentUserService currentUserService)
        : IRequestHandler<GetChannelUnreadCountQuery, int>
    {
        public async Task<int> Handle(GetChannelUnreadCountQuery query, CancellationToken ct)
        {
            var userId = await currentUserService.GetCurrentUserIdAsync();
            
            var count = await unreadTracker.GetCountAsync(userId, query.ChannelId, ct);

            return (int)count;
        }
    }
}