using codeTalks.Application.Features.Channels.Dtos;
using codeTalks.Application.Features.Channels.Models;
using codeTalks.Domain;
using Core.Persistence.Paging;
using Core.Security.Entities;
using Mapster;

namespace codeTalks.Application.Features.Channels.Profiles;

public class ChannelMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<Channel, ChannelsByUserIdItemDto>();
        config.NewConfig<IPaginate<Channel>, ChannelsByUserIdListModel>()
            .TwoWays();
        config.NewConfig<Role, UserRoleAtChannelDto>();
        
        config.NewConfig<Role, ChannelsByUserIdRoleDto>();
    }
}