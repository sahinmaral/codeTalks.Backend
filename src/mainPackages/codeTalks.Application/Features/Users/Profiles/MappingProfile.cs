using AutoMapper;
using codeTalks.Application.Features.Users.Dtos;
using codeTalks.Application.Features.Users.Models;
using codeTalks.Domain;
using Core.Persistence.Paging;
using Core.Security.Entities;

namespace codeTalks.Application.Features.Users.Profiles;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<IPaginate<ChannelUser>, UsersByChannelIdListModel>().ReverseMap();
        CreateMap<Role, UserRoleAtChannelDto>();
        CreateMap<ChannelUser, UsersByChannelIdDto>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.UserId))
            .ForMember(dest => dest.FirstName, opt => opt.MapFrom(src => src.User.FirstName))
            .ForMember(dest => dest.MiddleName, opt => opt.MapFrom(src => src.User.MiddleName))
            .ForMember(dest => dest.LastName, opt => opt.MapFrom(src => src.User.LastName))
            .ForMember(dest => dest.ProfilePhotoURL, opt => opt.MapFrom(src => src.User.ProfilePhotoURL))
            .ForMember(dest => dest.UserName, opt => opt.MapFrom(src => src.User.UserName))
            .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.User.Email));
    }
}