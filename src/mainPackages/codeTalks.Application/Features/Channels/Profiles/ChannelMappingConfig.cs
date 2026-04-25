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
        config.NewConfig<ChannelUser, GetUsersDetailAtChannelByChannelIdDto>()
            .Map(dest => dest.Id,src => src.UserId)
            .Map(dest => dest.FirstName,src => src.User.FirstName)
            .Map(dest => dest.MiddleName,src => src.User.MiddleName)
            .Map(dest => dest.LastName,src => src.User.LastName)
            .Map(dest => dest.ProfilePhotoURL,src => src.User.ProfilePhotoURL)
            .Map(dest => dest.UserName,src => src.User.UserName)
            .Map(dest => dest.Email,src => src.User.Email);
        config.NewConfig<Role, UserRoleAtChannelDto>();
        
        config.NewConfig<Role, ChannelsByUserIdRoleDto>();
    }
}