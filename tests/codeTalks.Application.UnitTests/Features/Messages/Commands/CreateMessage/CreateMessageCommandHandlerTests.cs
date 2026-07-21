using System.Linq.Expressions;
using codeTalks.Application.Features.Auths.Rules;
using codeTalks.Application.Features.Channels.Rules;
using codeTalks.Application.Features.Messages.Commands.CreateMessage;
using codeTalks.Application.Services;
using codeTalks.Application.Services.Notifications.Interfaces;
using codeTalks.Application.Services.Notifications.Models;
using codeTalks.Application.Services.Repositories;
using codeTalks.Application.UnitTests.TestUtilities;
using codeTalks.Domain;
using Core.CrossCuttingConcerns.Exceptions;
using Core.Security.Entities;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using NSubstitute;

namespace codeTalks.Application.UnitTests.Features.Messages.Commands.CreateMessage;

// CreateMessage validates the sender and channel exist (via the concrete business-rule
// classes over mocked repositories), persists the message, and publishes a
// ChannelMessageCreatedEvent enriched with the channel name and sender name.
public class CreateMessageCommandHandlerTests
{
    private const string ChannelId = "channel-1";
    private const string CurrentUserId = "sender-user";
    private const string SenderName = "jane";
    private const string ChannelName = "General";

    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();
    private readonly IMessageRepository _messageRepository = Substitute.For<IMessageRepository>();
    private readonly UserManager<User> _userManager = UserManagerMock.Create();
    private readonly IChannelRepository _channelRepository = Substitute.For<IChannelRepository>();
    private readonly IMessagePublisher _publisher = Substitute.For<IMessagePublisher>();
    private readonly CreateMessageCommand.CreateMessageCommandHandler _handler;

    private readonly User _sender = new() { Id = CurrentUserId, UserName = SenderName };
    private readonly Channel _channel = new() { Id = ChannelId, Name = ChannelName };

    public CreateMessageCommandHandlerTests()
    {
        _currentUserService.GetCurrentUserIdAsync().Returns(CurrentUserId);
        // AuthBusinessRules / ChannelBusinessRules are concrete; drive them via their repositories.
        _userManager.FindByIdAsync(CurrentUserId).Returns(_sender);
        _channelRepository.GetAsync(Arg.Any<Expression<Func<Channel, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(_channel);
        // the repository assigns identity/timestamps; echo the entity back so the event can read them
        _messageRepository.AddAsync(Arg.Any<Message>(), Arg.Any<CancellationToken>())
            .Returns(ci => ci.Arg<Message>());

        var authBusinessRules = new AuthBusinessRules(_userManager);
        var channelBusinessRules = new ChannelBusinessRules(_channelRepository);
        _handler = new CreateMessageCommand.CreateMessageCommandHandler(
            _currentUserService, _messageRepository, authBusinessRules, _publisher, channelBusinessRules);
    }

    private static CreateMessageCommand Command(string content = "Hello") =>
        new() { Content = content, ChannelId = ChannelId };

    [Fact]
    public async Task Handle_WhenSenderAndChannelExist_PersistsMessageForCurrentUserAndChannel()
    {
        await _handler.Handle(Command("Hello team"), CancellationToken.None);

        await _messageRepository.Received(1).AddAsync(
            Arg.Is<Message>(m =>
                m.Content == "Hello team" &&
                m.SenderId == CurrentUserId &&
                m.ChannelId == ChannelId),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenMessageIsCreated_PublishesEventEnrichedWithChannelAndSenderNames()
    {
        await _handler.Handle(Command("Hello team"), CancellationToken.None);

        await _publisher.Received(1).PublishAsync(
            Arg.Is<ChannelMessageCreatedEvent>(e =>
                e.ChannelId == ChannelId &&
                e.ChannelName == ChannelName &&
                e.SenderId == CurrentUserId &&
                e.SenderName == SenderName &&
                e.Content == "Hello team"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenSenderDoesNotExist_ThrowsEntityNotFoundAndDoesNotPersistOrPublish()
    {
        _userManager.FindByIdAsync(CurrentUserId).Returns((User?)null);

        var act = () => _handler.Handle(Command(), CancellationToken.None);

        await act.Should().ThrowAsync<EntityNotFoundException>().WithMessage("*User*");
        await _messageRepository.DidNotReceive().AddAsync(Arg.Any<Message>(), Arg.Any<CancellationToken>());
        await _publisher.DidNotReceive().PublishAsync(Arg.Any<ChannelMessageCreatedEvent>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenChannelDoesNotExist_ThrowsEntityNotFoundAndDoesNotPersistOrPublish()
    {
        _channelRepository.GetAsync(Arg.Any<Expression<Func<Channel, bool>>>(), Arg.Any<CancellationToken>())
            .Returns((Channel?)null);

        var act = () => _handler.Handle(Command(), CancellationToken.None);

        await act.Should().ThrowAsync<EntityNotFoundException>().WithMessage("*Channel*");
        await _messageRepository.DidNotReceive().AddAsync(Arg.Any<Message>(), Arg.Any<CancellationToken>());
        await _publisher.DidNotReceive().PublishAsync(Arg.Any<ChannelMessageCreatedEvent>(), Arg.Any<CancellationToken>());
    }
}