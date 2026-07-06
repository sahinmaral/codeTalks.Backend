
using codeTalks.Application.Services;
using codeTalks.Application.Services.Notifications.Interfaces;
using codeTalks.Application.Services.Repositories;
using codeTalks.Domain;
using Core.Application.CQRS;

namespace codeTalks.Application.Features.Users.Commands.UpdateUserNotificationSetting;

public class UpdateUserNotificationSettingCommand: ICommand
{
    public bool IsSoundEnabled { get; set; }
    
    public class UpdateUserNotificationSettingCommandHandler(
        ICurrentUserService currentUserService,
        IUserNotificationSettingRepository userNotificationSettingRepository,
        IUserSettingsCache settingsCache
        ) : ICommandHandler<UpdateUserNotificationSettingCommand>
    {
        public async Task<Unit> Handle(UpdateUserNotificationSettingCommand request, CancellationToken cancellationToken)
        {
            var currentUserId = await currentUserService.GetCurrentUserIdAsync();
            
            var userNotificationSetting = await userNotificationSettingRepository
                .GetByUserIdAsync(currentUserId, cancellationToken);

            if (userNotificationSetting is null)
            {
                userNotificationSetting = new UserNotificationSetting
                {
                    UserId = currentUserId,
                    IsEnabled = true,
                    IsSoundEnabled = request.IsSoundEnabled,
                };

                await userNotificationSettingRepository.AddAsync(userNotificationSetting, cancellationToken);
            }
            else
            {
                userNotificationSetting.Update(userNotificationSetting.IsEnabled, request.IsSoundEnabled);

                await userNotificationSettingRepository.UpdateAsync(userNotificationSetting);
            }

            await settingsCache.SetNotificationSettingAsync(
                currentUserId,
                userNotificationSetting.IsEnabled, userNotificationSetting.IsSoundEnabled);

            return Unit.Value;
        }
    }
}