using AutoMapper;
using codeTalks.Application.Features.Channels.Dtos;
using codeTalks.Application.Features.Channels.Models;
using codeTalks.Domain;
using Core.Persistence.Paging;

namespace codeTalks.Application.Features.Channels.Profiles;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<Channel, ChannelsByUserIdItemDto>();
        CreateMap<IPaginate<Channel>, ChannelsByUserIdListModel>().ReverseMap();
    }
}