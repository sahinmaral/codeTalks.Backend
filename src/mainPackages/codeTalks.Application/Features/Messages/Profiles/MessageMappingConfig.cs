using codeTalks.Application.Features.Messages.Dtos;
using codeTalks.Application.Features.Messages.Models;
using codeTalks.Domain;
using Core.Persistence.Paging;
using Core.Security.Entities;
using Mapster;

namespace codeTalks.Application.Features.Messages.Profiles;

public class MessageMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<IPaginate<Message>, MessagesByChannelIdListModel>();
        config.NewConfig<Message, MessagesByChannelIdDto>();
        config.NewConfig<User, UserWhoWroteMessageByChannelIdDto>();
    }
}