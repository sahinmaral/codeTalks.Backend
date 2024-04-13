using AutoMapper;
using codeTalks.Application.Features.Auths.Commands.RegisterUser;
using codeTalks.Application.Features.Auths.Dtos;
using Core.Security.Entities;

namespace codeTalks.Application.Features.Auths.Profiles;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<RegisterUserCommand, User>();
        CreateMap<User, RegisteredUserDto>();

        CreateMap<User, LoggedUserDto>();
    }
}