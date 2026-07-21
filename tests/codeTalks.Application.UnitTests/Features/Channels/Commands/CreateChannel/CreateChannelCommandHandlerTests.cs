using System.Linq.Expressions;
using codeTalks.Application.Features.Channels.Commands.CreateChannel;
using codeTalks.Application.Services;
using codeTalks.Application.Services.Repositories;
using codeTalks.Application.UnitTests.TestUtilities;
using codeTalks.Domain;
using Core.Security.Entities;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using NSubstitute;

namespace codeTalks.Application.UnitTests.Features.Channels.Commands.CreateChannel;

// CreateChannel builds a channel, generates a unique invite code (retrying on collision),
// and enrolls the creator as an Accepted member. No validator exists for this command.
public class CreateChannelCommandHandlerTests
{
    private const string CurrentUserId = "creator-user";

    private static readonly Role OwnerRole = new() { Id = "role-owner", Name = "Owner" };

    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();
    private readonly IChannelRepository _channelRepository = Substitute.For<IChannelRepository>();
    private readonly RoleManager<Role> _roleManager = RoleManagerMock.Create();
    private readonly IInviteCodeGenerator _codeGenerator = Substitute.For<IInviteCodeGenerator>();
    private readonly CreateChannelCommand.CreateChannelCommandHandler _handler;

    private Channel? _addedChannel;

    public CreateChannelCommandHandlerTests()
    {
        _currentUserService.GetCurrentUserIdAsync().Returns(CurrentUserId);
        _roleManager.FindByNameAsync("Owner").Returns(OwnerRole);
        _codeGenerator.Generate().Returns("CODE1");
        // default: every generated code is unique (no existing channel found)
        _channelRepository.GetAsync(Arg.Any<Expression<Func<Channel, bool>>>(), Arg.Any<CancellationToken>())
            .Returns((Channel?)null);
        // capture whatever channel gets persisted so tests can assert on the built graph
        _channelRepository.AddAsync(Arg.Do<Channel>(c => _addedChannel = c), Arg.Any<CancellationToken>());

        _handler = new CreateChannelCommand.CreateChannelCommandHandler(
            _currentUserService, _channelRepository, _roleManager, _codeGenerator);
    }

    private static CreateChannelCommand Command(
        string name = "General",
        string description = "General chat",
        ChannelJoinPolicy joinPolicy = ChannelJoinPolicy.Request) =>
        new() { Name = name, Description = description, JoinPolicy = joinPolicy };

    [Fact]
    public async Task Handle_WhenCodeIsUniqueFirstTry_PersistsChannelWithRequestedFields()
    {
        await _handler.Handle(Command(name: "Devs", description: "Dev talk", joinPolicy: ChannelJoinPolicy.Open),
            CancellationToken.None);

        _addedChannel.Should().NotBeNull();
        _addedChannel!.Name.Should().Be("Devs");
        _addedChannel.Description.Should().Be("Dev talk");
        _addedChannel.JoinPolicy.Should().Be(ChannelJoinPolicy.Open);
        _addedChannel.InviteCode.Should().Be("CODE1");
        await _channelRepository.Received(1).AddAsync(Arg.Any<Channel>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenCodeIsUniqueFirstTry_EnrollsCreatorAsAcceptedMember()
    {
        await _handler.Handle(Command(), CancellationToken.None);

        _addedChannel!.ChannelUsers.Should().ContainSingle();
        var creatorMembership = _addedChannel.ChannelUsers.Single();
        creatorMembership.UserId.Should().Be(CurrentUserId);
        creatorMembership.Status.Should().Be(ChannelUserStatus.Accepted);
        // The creator becomes the channel Owner (the domain's top authority for
        // ChangeUserRole / LeaveChannel / PatchUserStatus).
        creatorMembership.RoleId.Should().Be(OwnerRole.Id);
    }

    [Fact]
    public async Task Handle_WhenGeneratedCodeCollides_RetriesUntilUnique()
    {
        _codeGenerator.Generate().Returns("DUP", "UNIQUE");
        // first lookup finds an existing channel (collision), second finds none (unique)
        _channelRepository.GetAsync(Arg.Any<Expression<Func<Channel, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(new Channel(), (Channel?)null);

        await _handler.Handle(Command(), CancellationToken.None);

        _addedChannel!.InviteCode.Should().Be("UNIQUE");
        _codeGenerator.Received(2).Generate();
        await _channelRepository.Received(1).AddAsync(Arg.Any<Channel>(), Arg.Any<CancellationToken>());
    }
}