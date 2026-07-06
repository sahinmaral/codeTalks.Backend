using codeTalks.Application.Services;
using codeTalks.Application.Services.Notifications.Interfaces;
using Core.Application.CQRS;

namespace codeTalks.Application.Features.Notifications.Commands;

public class ResetChannelUnreadCountCommand : ICommand
{
    public string ChannelId { get; set; }
    
    public class ResetChannelUnreadCountCommandHandler( IUnreadTracker unreadTracker,
        ICurrentUserService currentUserService) : ICommandHandler<ResetChannelUnreadCountCommand>
    {
        public async Task<Unit> Handle(ResetChannelUnreadCountCommand request, CancellationToken cancellationToken)
        {
            var userId = await currentUserService.GetCurrentUserIdAsync();
            await unreadTracker.ResetAsync(userId, request.ChannelId, cancellationToken);
            return Unit.Value;
        }
    }
}