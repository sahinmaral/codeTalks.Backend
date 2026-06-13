using System.Linq.Expressions;
using codeTalks.Application.Features.Channels.Dtos;
using codeTalks.Application.Services;
using codeTalks.Application.Services.Repositories;
using codeTalks.Domain;
using Core.Application.CQRS;
using Core.CrossCuttingConcerns.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace codeTalks.Application.Features.Channels.Queries.GetUsersDetailAtChannelByChannelId;

public class GetUsersByChannelIdQuery : IRequest<UsersAtChannelListModel>
{
    public required string ChannelId { get; set; }
    public ChannelUserStatus Status { get; set; }
    public string? Search { get; init; }
    public int Index { get; init; }
    public int Size { get; init; } = 10;

    public class GetUsersByChannelIdQueryHandler(
        IChannelRepository channelRepository,
        ICurrentUserService currentUserService)
        : IRequestHandler<GetUsersByChannelIdQuery, UsersAtChannelListModel>
    {
        public async Task<UsersAtChannelListModel> Handle(GetUsersByChannelIdQuery request, CancellationToken cancellationToken)
        {
            var currentUserId = await currentUserService.GetCurrentUserIdAsync();

            await CheckCurrentUserCanAccessChannel(request.ChannelId, currentUserId, cancellationToken);
            
            IList<UsersAtChannelDto>? admins = null;
            
            if (request.Status == ChannelUserStatus.Accepted)
            {
                admins = await channelRepository.GetChannelAdminsAsync(
                    selector: ToDto(),
                    channelId: request.ChannelId,
                    cancellationToken: cancellationToken
                );   
            }

            var members = await channelRepository.GetChannelUsersAsync(
                selector: ToDto(),
                channelId: request.ChannelId,
                status: request.Status,
                excludeRoleName: "Moderator",
                search: request.Search,
                index: request.Index,
                size: request.Size,
                cancellationToken: cancellationToken
            );
            
            return new UsersAtChannelListModel
            {
                Admins = admins,
                Items = members.Items,
                Count = members.Count,
                Index = members.Index,
                Size = members.Size,
                Pages = members.Pages,
                HasPrevious = members.HasPrevious,
                HasNext = members.HasNext
            };
        }

        private static Expression<Func<ChannelUser, UsersAtChannelDto>> ToDto() =>
            cu => new UsersAtChannelDto
            {
                Id = cu.UserId,
                FirstName = cu.User.FirstName,
                MiddleName = cu.User.MiddleName,
                LastName = cu.User.LastName,
                ProfilePhotoURL = cu.User.ProfilePhotoURL,
                UserName = cu.User.UserName!,
                Email = cu.User.Email!,
                Role = new UserRoleAtChannelDto
                {
                    Id = cu.Role.Id,
                    Name = cu.Role.Name!
                },
                StatusCreatedAt = cu.CreatedAt,
            };

        private async Task CheckCurrentUserCanAccessChannel(string channelId, string currentUserId, CancellationToken cancellationToken)
        {
            var channel = await channelRepository.GetDetailedAsync(
                include: q => q.Include(c => c.ChannelUsers),
                predicate: c => c.Id == channelId,
                cancellationToken: cancellationToken
            );

            if (channel is null)
                throw new EntityNotFoundException("This channel doesn't exist");

            var currentUserAtChannel = channel.ChannelUsers
                .FirstOrDefault(cu => cu.UserId == currentUserId && cu.Status == ChannelUserStatus.Accepted);

            if (currentUserAtChannel is null)
                throw new BusinessException("You are not authorized to see this channel");
        }
    }
}