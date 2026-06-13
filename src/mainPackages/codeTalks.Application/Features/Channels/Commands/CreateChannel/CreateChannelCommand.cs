using codeTalks.Application.Services;
using codeTalks.Application.Services.Repositories;
using codeTalks.Domain;
using Core.Application.CQRS;
using Core.Security.Entities;
using Microsoft.AspNetCore.Identity;

namespace codeTalks.Application.Features.Channels.Commands.CreateChannel;

public class CreateChannelCommand : ICommand
{
    public string Name { get; set; }
    public string Description { get; set; }
    
    public class CreateChannelCommandHandler(
        ICurrentUserService currentUserService,
        IChannelRepository channelRepository,
        RoleManager<Role> roleManager,
        IInviteCodeGenerator codeGenerator) : ICommandHandler<CreateChannelCommand>
    {
        public async Task<Unit> Handle(CreateChannelCommand request, CancellationToken cancellationToken)
        {
            var currentUserId = await currentUserService.GetCurrentUserIdAsync();
            
            string inviteCode;
            bool isUnique;
            
            do {
                inviteCode = codeGenerator.Generate();
                isUnique = await channelRepository.GetAsync(x => x.InviteCode == inviteCode, cancellationToken) == null;
            } while (!isUnique);
            
            var moderatorRole = await roleManager.FindByNameAsync("Moderator");

            var newChannel = new Channel
            {
                Name = request.Name,
                Description = request.Description,
                InviteCode = inviteCode,
                ChannelUsers = new List<ChannelUser>()
            };

            newChannel.ChannelUsers.Add(new ChannelUser
            {
                ChannelId = newChannel.Id,
                UserId = currentUserId,
                Status = ChannelUserStatus.Accepted,
                RoleId = moderatorRole!.Id
            });

            await channelRepository.AddAsync(newChannel);
            return Unit.Value;
        }
    }
}