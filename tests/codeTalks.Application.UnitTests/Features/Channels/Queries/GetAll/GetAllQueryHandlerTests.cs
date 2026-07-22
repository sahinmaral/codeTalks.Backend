using System.Linq.Expressions;
using codeTalks.Application.Features.Auths.Rules;
using codeTalks.Application.Features.Channels.Dtos;
using codeTalks.Application.Features.Channels.Models;
using codeTalks.Application.Features.Channels.Queries.GetAll;
using codeTalks.Application.Services;
using codeTalks.Application.Services.Repositories;
using codeTalks.Application.UnitTests.TestUtilities;
using codeTalks.Domain;
using Core.CrossCuttingConcerns.Exceptions;
using Core.Persistence.Paging;
using Core.Security.Entities;
using FluentAssertions;
using MapsterMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore.Query;
using NSubstitute;

namespace codeTalks.Application.UnitTests.Features.Channels.Queries.GetAll;

// GetAllQuery serves two different callers with the same handler: ChannelsController
// (Status left unset -> discovery list of channels the user hasn't joined) and
// ChatHub.SendActiveChannelsByUserId (Status = Accepted -> the user's own active
// channels). Either way, the handler hand-computes MemberCount/Status/Role per item
// after the repository call - that computed logic is what these tests cover.
public class GetAllQueryHandlerTests
{
    private const string CurrentUserId = "current-user";

    private static readonly Role OwnerRole = new() { Id = "role-owner", Name = "Owner" };

    private readonly IChannelRepository _channelRepository = Substitute.For<IChannelRepository>();
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();
    private readonly IMapper _mapper = Substitute.For<IMapper>();
    private readonly UserManager<User> _userManager = UserManagerMock.Create();
    private readonly GetAllQuery.GetAllQueryHandler _handler;

    public GetAllQueryHandlerTests()
    {
        _currentUserService.GetCurrentUserIdAsync().Returns(CurrentUserId);
        _userManager.FindByIdAsync(CurrentUserId).Returns(new User { Id = CurrentUserId });
        var authBusinessRules = new AuthBusinessRules(_userManager);
        _handler = new GetAllQuery.GetAllQueryHandler(
            _currentUserService, _channelRepository, _mapper, authBusinessRules);
    }

    private static ChannelUser Member(string userId, ChannelUserStatus status) =>
        new() { UserId = userId, Status = status, Role = OwnerRole, RoleId = OwnerRole.Id };

    private Channel SetupRepositoryToReturn(Channel channel)
    {
        _channelRepository.GetListAsync(
                predicate: Arg.Any<Expression<Func<Channel, bool>>>(),
                orderBy: Arg.Any<Func<IQueryable<Channel>, IOrderedQueryable<Channel>>>(),
                include: Arg.Any<Func<IQueryable<Channel>, IIncludableQueryable<Channel, object>>>(),
                index: Arg.Any<int>(),
                size: Arg.Any<int>(),
                enableTracking: Arg.Any<bool>(),
                cancellationToken: Arg.Any<CancellationToken>())
            .Returns(new Paginate<Channel> { Items = new List<Channel> { channel } });

        var itemDto = new ChannelsByUserIdItemDto { Id = channel.Id };
        _mapper.Map<ChannelsByUserIdListModel>(Arg.Any<IPaginate<Channel>>())
            .Returns(new ChannelsByUserIdListModel { Items = new List<ChannelsByUserIdItemDto> { itemDto } });
        _mapper.Map<ChannelsByUserIdRoleDto>(Arg.Any<Role>())
            .Returns(new ChannelsByUserIdRoleDto { Id = OwnerRole.Id, Name = OwnerRole.Name });

        return channel;
    }

    [Fact]
    public async Task Handle_WhenStatusNotSet_ComputesMemberCountUsingAcceptedByDefault()
    {
        var channel = SetupRepositoryToReturn(new Channel
        {
            Id = "channel-1",
            ChannelUsers = new List<ChannelUser>
            {
                Member(CurrentUserId, ChannelUserStatus.Accepted),
                Member("u2", ChannelUserStatus.Accepted),
                Member("u3", ChannelUserStatus.Accepted),
                Member("u4", ChannelUserStatus.Banned), // not counted
            }
        });

        var result = await _handler.Handle(new GetAllQuery(), CancellationToken.None);

        result.Items[0].MemberCount.Should().Be(3, "only Accepted members are counted when Status is unset");
        result.Items[0].Status.Should().Be(ChannelUserStatus.Accepted, "the current user's own membership status");
        result.Items[0].Role!.Id.Should().Be(OwnerRole.Id);
    }

    [Fact]
    public async Task Handle_WhenStatusSet_ComputesMemberCountUsingThatStatusInstead()
    {
        // Mirrors ChatHub.SendActiveChannelsByUserId, which always sets Status = Accepted.
        SetupRepositoryToReturn(new Channel
        {
            Id = "channel-1",
            ChannelUsers = new List<ChannelUser>
            {
                Member(CurrentUserId, ChannelUserStatus.RequestSent),
                Member("u2", ChannelUserStatus.RequestSent),
                Member("u3", ChannelUserStatus.Accepted), // not counted for this request
            }
        });

        var result = await _handler.Handle(
            new GetAllQuery { Status = ChannelUserStatus.RequestSent }, CancellationToken.None);

        result.Items[0].MemberCount.Should().Be(2, "MemberCount counts the requested Status, not the Accepted default");
    }

    [Fact]
    public async Task Handle_WhenCurrentUserIsNotAMember_LeavesStatusAndRoleNull()
    {
        SetupRepositoryToReturn(new Channel
        {
            Id = "channel-1",
            ChannelUsers = new List<ChannelUser> { Member("someone-else", ChannelUserStatus.Accepted) }
        });

        var result = await _handler.Handle(new GetAllQuery(), CancellationToken.None);

        result.Items[0].Status.Should().BeNull();
        result.Items[0].Role.Should().BeNull();
    }

    [Fact]
    public async Task Handle_WhenCurrentUserDoesNotExist_ThrowsEntityNotFound()
    {
        _userManager.FindByIdAsync(CurrentUserId).Returns((User?)null);

        var act = () => _handler.Handle(new GetAllQuery(), CancellationToken.None);

        await act.Should().ThrowAsync<EntityNotFoundException>();
    }
}
