using codeTalks.Application.Features.Auths.Rules;
using codeTalks.Application.Features.Channels.Dtos;
using codeTalks.Application.Features.Channels.Rules;
using codeTalks.Application.Services;
using codeTalks.Application.Services.Notifications.Interfaces;
using codeTalks.Application.Services.Notifications.Models;
using codeTalks.Application.Services.Repositories;
using codeTalks.Domain;
using Core.Application.CQRS;

namespace codeTalks.Application.Features.Messages.Commands.CreateMessage;

public class CreateMessageCommand : ICommand
{
    public string Content { get; set; }
    public string ChannelId { get; set; }
    
    public class CreateMessageCommandHandler(
        ICurrentUserService currentUserService,
        IMessageRepository messageRepository,
        AuthBusinessRules authBusinessRules,
        IMessagePublisher publisher,
        ChannelBusinessRules channelBusinessRules) : ICommandHandler<CreateMessageCommand>
    {
        public async Task<Unit> Handle(CreateMessageCommand request, CancellationToken cancellationToken)
        {
            var currentUserId = await currentUserService.GetCurrentUserIdAsync();
            
            var sender = await authBusinessRules.CheckUserExistsById(currentUserId);
            var channel = await channelBusinessRules.CheckChannelExistsById(request.ChannelId);

            var message = await messageRepository.AddAsync(new Message
            {
                Content   = request.Content,
                SenderId  = currentUserId,
                ChannelId = request.ChannelId
            }, cancellationToken);
            
            await publisher.PublishAsync(new ChannelMessageCreatedEvent(
                message.Id,
                message.ChannelId,
                channel.Name, 
                message.SenderId,
                sender.UserName!,
                message.Content,
                message.CreatedAt
            ), cancellationToken);
            
            return Unit.Value;
        }
    }
}