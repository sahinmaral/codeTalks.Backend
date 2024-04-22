using AutoMapper;
using codeTalks.Application.Features.Channels.Dtos;
using codeTalks.Application.Services.Repositories;
using codeTalks.Domain;
using Core.CrossCuttingConcerns.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace codeTalks.Application.Features.Channels.Queries.GetUsersDetailAtChannelByChannelId;

public class GetUsersDetailAtChannelByChannelIdQuery : IRequest<GetUsersDetailAtChannelByChannelIdDto>
{
    public string UserId { get; set; }
    public string ChannelId { get; set; }
    
    public class GetUsersDetailAtChannelByChannelIdQueryHandler(IChannelRepository channelRepository,IMapper mapper) : IRequestHandler<GetUsersDetailAtChannelByChannelIdQuery, GetUsersDetailAtChannelByChannelIdDto>
    {
        public async Task<GetUsersDetailAtChannelByChannelIdDto> Handle(GetUsersDetailAtChannelByChannelIdQuery request, CancellationToken cancellationToken)
        {
            var existedChannel = await channelRepository.GetDetailedAsync(
                include: queryable => queryable
                    .Include(channel => channel.ChannelUsers)
                        .ThenInclude(channelUser => channelUser.User)
                    .Include(channel => channel.ChannelUsers)
                        .ThenInclude(channelUser => channelUser.Role),
                predicate: channel => channel.Id == request.ChannelId
            );

            if (existedChannel is null)
                throw new EntityNotFoundException("This channel doesn't exist");

            var foundUserAtChannel = existedChannel.ChannelUsers.FirstOrDefault(channelUser => channelUser.UserId == request.UserId);

            if (foundUserAtChannel is null)
                throw new EntityNotFoundException("This user hasn't registered this channel yet");
            
            if(foundUserAtChannel.Status != ChannelUserStatus.Accepted)
                throw new EntityNotFoundException("This user hasn't accepted this channel yet"); 
            
            return mapper.Map<GetUsersDetailAtChannelByChannelIdDto>(foundUserAtChannel);
        }
    }
}