using codeTalks.Application.Features.Channels.Dtos;
using codeTalks.Application.Features.Messages.Dtos;
using codeTalks.Application.Features.Messages.Models;
using codeTalks.Application.Services.Repositories;
using Core.Application.CQRS;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;

namespace codeTalks.Application.Features.Messages.Queries.GetAllByChannelId;

public class GetAllByChannelIdQuery : IRequest<MessagesByChannelIdListModel>
{
    public string ChannelId { get; set; }
    public int Size { get; set; }
    public int Index { get; set; }

    public class GetAllByChannelIdQueryHandler(IMessageRepository messageRepository, IMapper mapper)
        : IRequestHandler<GetAllByChannelIdQuery, MessagesByChannelIdListModel>
    {
        public async Task<MessagesByChannelIdListModel> Handle(GetAllByChannelIdQuery request, CancellationToken cancellationToken)
        {
            var messages = await messageRepository.GetListAsync(
                size: request.Size,
                index: request.Index,
                predicate: message => message.ChannelId == request.ChannelId,
                orderBy: queryable => queryable.OrderByDescending(message => message.CreatedAt),
                include: queryable => queryable.Include(message => message.Sender),
                cancellationToken: cancellationToken);

            return new MessagesByChannelIdListModel
            {
                Items = messages.Items
                    .Reverse()
                    .Select(mapper.Map<MessagesByChannelIdDto>)
                    .ToList(),
                Count = messages.Count,
                Index = messages.Index,
                Size = messages.Size,
                Pages = messages.Pages,
            };
        }
    }
}