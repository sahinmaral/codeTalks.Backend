using codeTalks.Application.Features.Auths.Commands.RegisterUser;
using codeTalks.Application.Features.Auths.Dtos;
using codeTalks.Domain;
using Core.Security.Entities;
using Mapster;

namespace codeTalks.Application.Features.Auths.Profiles;

public class AuthMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<RegisterUserCommand, User>();
        config.NewConfig<User, RegisteredUserDto>();
        config.NewConfig<User, LoggedUserDto>();
        config.NewConfig<User, GetCurrentUserDto>();

        config.NewConfig<UserStatus, GetUserStatusDto>()
              .Map(dest => dest.LastUpdated, src => src.UpdatedAt ?? src.CreatedAt);
    }
}