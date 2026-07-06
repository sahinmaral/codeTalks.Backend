using codeTalks.Application.Features.Auths.Rules;
using codeTalks.Application.Features.Users.Dtos;
using codeTalks.Application.Services;
using codeTalks.Application.Services.Repositories;
using codeTalks.Domain;
using Core.Application.CQRS;
using Core.CrossCuttingConcerns.Exceptions;
using MapsterMapper;

namespace codeTalks.Application.Features.Users.Queries.GetUserNotificationSettings;

public class GetUserNotificationSettingsQuery: IRequest<UserNotificationSettingDto>
{
    public class GetUserNotificationSettingsQueryHandler(
        ICurrentUserService currentUserService,
        IUserNotificationSettingRepository userNotificationSettingRepository,
        IMapper mapper,
        AuthBusinessRules authBusinessRules) : IRequestHandler<GetUserNotificationSettingsQuery, UserNotificationSettingDto>
    {
        public async Task<UserNotificationSettingDto> Handle(GetUserNotificationSettingsQuery request, CancellationToken cancellationToken)
        {
            var currentUserId = await currentUserService.GetCurrentUserIdAsync();
            
            await authBusinessRules.CheckUserExistsById(currentUserId);

            var userNotificationSetting = await userNotificationSettingRepository
                .GetAsync(x => x.UserId == currentUserId, cancellationToken);

            if (userNotificationSetting == null)
                throw new EntityNotFoundException("Your notification settings does not exist");
            
            var mappedEntity = mapper.Map<UserNotificationSettingDto>(userNotificationSetting);

            return mappedEntity;
        }
    }
}