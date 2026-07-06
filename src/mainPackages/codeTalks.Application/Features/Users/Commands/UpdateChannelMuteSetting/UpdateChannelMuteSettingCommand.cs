using codeTalks.Application.Services;
using codeTalks.Application.Services.Notifications.Interfaces;
using codeTalks.Application.Services.Repositories;
using codeTalks.Domain;
using Core.Application.CQRS;
using Core.CrossCuttingConcerns.Exceptions;
using MapsterMapper;

namespace codeTalks.Application.Features.Users.Commands.UpdateChannelMuteSetting;

public class UpdateChannelMuteSettingCommand: ICommand
{
    public string ChannelId { get; set; }
    public DateTime MuteUntil { get; set; } = DateTime.Now;
    
    public class UpdateChannelMuteSettingCommandHandler(
        ICurrentUserService currentUserService,
        IUserChannelMuteSettingRepository userChannelMuteSettingRepository,
        IUserSettingsCache settingsCache
        ) : ICommandHandler<UpdateChannelMuteSettingCommand>
    {
        public async Task<Unit> Handle(UpdateChannelMuteSettingCommand request, CancellationToken cancellationToken)
        {
            var currentUserId = await currentUserService.GetCurrentUserIdAsync();
            
            var userChannelMuteSetting = await userChannelMuteSettingRepository
                .GetAsync(currentUserId, request.ChannelId, cancellationToken);

            if (userChannelMuteSetting is null)
            {
                userChannelMuteSetting = new UserChannelMuteSetting
                {
                    ChannelId = request.ChannelId,
                    UserId = currentUserId
                };

                userChannelMuteSetting.Mute(request.MuteUntil);

                await userChannelMuteSettingRepository.AddAsync(userChannelMuteSetting, cancellationToken);
            }
            else
            {
                userChannelMuteSetting.Mute(request.MuteUntil);

                await userChannelMuteSettingRepository.UpdateAsync(userChannelMuteSetting);
            }

            await settingsCache.SetChannelMuteSettingAsync(
                currentUserId, request.ChannelId,
                userChannelMuteSetting.IsMuted, userChannelMuteSetting.MutedUntil);

            return Unit.Value;
        }
    }
}