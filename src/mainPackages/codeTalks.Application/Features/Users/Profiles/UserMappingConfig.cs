using codeTalks.Application.Features.Users.Dtos;
using codeTalks.Application.Features.Users.Models;
using codeTalks.Domain;
using Core.Persistence.Paging;
using Core.Security.Entities;
using Mapster;

namespace codeTalks.Application.Features.Users.Profiles;

public class UserMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<IPaginate<ChannelUser>, UsersByChannelIdListModel>().TwoWays();
        config.NewConfig<Role, UserRoleAtChannelDto>();
        
        config.NewConfig<ChannelUser, UsersByChannelIdDto>()
            .Map(dest => dest.Id, src => src.UserId)
            .Map(dest => dest.FirstName, src => src.User.FirstName)
            .Map(dest => dest.MiddleName, src => src.User.MiddleName)
            .Map(dest => dest.LastName, src => src.User.LastName)
            .Map(dest => dest.ProfilePhotoURL, src => src.User.ProfilePhotoURL)
            .Map(dest => dest.UserName, src => src.User.UserName)
            .Map(dest => dest.Email, src => src.User.Email);
    }
}