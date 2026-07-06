using codeTalks.Application.Features.Auths.Commands.RegisterUser;
using codeTalks.Application.Features.Auths.Dtos;
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
    }
}