using codeTalks.Application.Services;
using codeTalks.Application.Services.Notifications.Interfaces;
using codeTalks.Application.Services.Repositories;
using Core.Application.CQRS;
using Core.CrossCuttingConcerns.Exceptions;

namespace codeTalks.Application.Features.Users.Commands.UnmuteChannel;

public class UnmuteChannelCommand : ICommand
{
    public string ChannelId { get; set; }

    public class UnmuteChannelCommandHandler(
        ICurrentUserService currentUserService,
        IUserChannelMuteSettingRepository userChannelMuteSettingRepository,
        IUserSettingsCache settingsCache
        ) : ICommandHandler<UnmuteChannelCommand>
    {
        public async Task<Unit> Handle(UnmuteChannelCommand request, CancellationToken cancellationToken)
        {
            var currentUserId = await currentUserService.GetCurrentUserIdAsync();

            var userChannelMuteSetting = await userChannelMuteSettingRepository
                .GetAsync(currentUserId, request.ChannelId, cancellationToken);

            if (userChannelMuteSetting is null)
                throw new BusinessException("You haven't set this channel as muted yet.");

            await userChannelMuteSettingRepository.DeleteAsync(userChannelMuteSetting, cancellationToken);
            
            await settingsCache.InvalidateMuteSettingAsync(currentUserId, request.ChannelId);

            return Unit.Value;
        }
    }
}
