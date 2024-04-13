using codeTalks.Application.Features.Auths.Rules;
using codeTalks.Application.Features.Channels.Rules;
using codeTalks.Application.Services.Repositories;
using codeTalks.Domain;
using MediatR;

namespace codeTalks.Application.Features.Messages.Commands.CreateMessage;

public class CreateMessageCommand : IRequest
{
    public string Content { get; set; }
    public string UserId { get; set; }
    public string ChannelId { get; set; }
    
    public class CreateMessageCommandHandler(
        IMessageRepository messageRepository,
        AuthBusinessRules authBusinessRules,
        ChannelBusinessRules channelBusinessRules) : IRequestHandler<CreateMessageCommand>
    {
        public async Task Handle(CreateMessageCommand request, CancellationToken cancellationToken)
        {
            await authBusinessRules.CheckUserExistsById(request.UserId);
            await channelBusinessRules.CheckChannelExistsById(request.ChannelId);

            await messageRepository.AddAsync(new Message
            {
                Content = request.Content,
                SenderId = request.UserId,
                ChannelId = request.ChannelId
            });
        }
    }
}