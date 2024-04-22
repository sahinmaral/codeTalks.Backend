using codeTalks.Application.Features.Auths.Rules;
using codeTalks.Application.Services.Repositories;
using codeTalks.Domain;
using Core.Security.Entities;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace codeTalks.Application.Features.Channels.Commands.CreateChannel;

public class CreateChannelCommand : IRequest
{
    public string Name { get; set; }
    public string Description { get; set; }
    public string UserId { get; set; }
    
    public class CreateChannelCommandHandler(
        IChannelRepository channelRepository,
        RoleManager<Role> roleManager,
        AuthBusinessRules authBusinessRules) : IRequestHandler<CreateChannelCommand>
    {
        public async Task Handle(CreateChannelCommand request, CancellationToken cancellationToken)
        {
            var user = await authBusinessRules.CheckUserExistsById(request.UserId);
            var moderatorRole = await roleManager.FindByNameAsync("Moderator");

            var newChannel = new Channel
            {
                Name = request.Name,
                Description = request.Description,
                ChannelUsers = new List<ChannelUser>()
            };

            newChannel.ChannelUsers.Add(new ChannelUser
            {
                ChannelId = newChannel.Id,
                UserId = user.Id,
                Status = ChannelUserStatus.Accepted,
                RoleId = moderatorRole!.Id
            });

            await channelRepository.AddAsync(newChannel);
        }
    }
}