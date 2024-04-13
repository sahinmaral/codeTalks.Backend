using AutoMapper;
using codeTalks.Application.Features.Messages.Dtos;
using codeTalks.Application.Features.Messages.Models;
using codeTalks.Domain;
using Core.Persistence.Paging;
using Core.Security.Entities;

namespace codeTalks.Application.Features.Messages.Profiles;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<IPaginate<Message>, MessagesByChannelIdListModel>();
        CreateMap<Message, MessagesByChannelIdDto>();
        CreateMap<User, UserWhoWroteMessageByChannelIdDto>();
    }
}