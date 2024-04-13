using AutoMapper;
using codeTalks.Application.Features.Messages.Models;
using codeTalks.Application.Services.Repositories;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace codeTalks.Application.Features.Messages.Queries.GetAllByChannelId;

public class GetAllByChannelIdQuery : IRequest<MessagesByChannelIdListModel>
{
    public string ChannelId { get; set; }

    public class GetAllByChannelIdQueryHandler(IMessageRepository messageRepository, IMapper mapper)
        : IRequestHandler<GetAllByChannelIdQuery, MessagesByChannelIdListModel>
    {
        public async Task<MessagesByChannelIdListModel> Handle(GetAllByChannelIdQuery request, CancellationToken cancellationToken)
        {
            var userAddictionLevel = await messageRepository.GetListAsync(
                predicate:message => message.ChannelId == request.ChannelId,
                orderBy: queryable => queryable.OrderBy(message => message.CreatedAt),
                include: queryable => queryable.Include(message => message.Sender), cancellationToken: cancellationToken);
            return mapper.Map<MessagesByChannelIdListModel>(userAddictionLevel);
        }
    }
}