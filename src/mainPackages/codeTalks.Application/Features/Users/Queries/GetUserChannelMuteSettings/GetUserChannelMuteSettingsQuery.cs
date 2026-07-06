using codeTalks.Application.Features.Auths.Rules;
using codeTalks.Application.Features.Users.Dtos;
using codeTalks.Application.Services;
using codeTalks.Application.Services.Repositories;
using Core.Application.CQRS;
using MapsterMapper;

namespace codeTalks.Application.Features.Users.Queries.GetUserChannelMuteSettings;

public class GetUserChannelMuteSettingsQuery: IRequest<List<UserChannelMuteSettingDto>>
{
    public class GetUserChannelMuteSettingsQueryHandler(
        ICurrentUserService currentUserService,
        IUserChannelMuteSettingRepository userChannelMuteSettingRepository,
        IMapper mapper,
        AuthBusinessRules authBusinessRules) : IRequestHandler<GetUserChannelMuteSettingsQuery, List<UserChannelMuteSettingDto>>
    {
        public async Task<List<UserChannelMuteSettingDto>> Handle(GetUserChannelMuteSettingsQuery request, CancellationToken cancellationToken)
        {
            var currentUserId = await currentUserService.GetCurrentUserIdAsync();
            
            await authBusinessRules.CheckUserExistsById(currentUserId);

            var userChannelMuteSettings = await userChannelMuteSettingRepository
                .GetAllAsync(userId: currentUserId, cancellationToken);
            
            var response = mapper.Map<List<UserChannelMuteSettingDto>>(userChannelMuteSettings);

            return response;
        }
    }
}