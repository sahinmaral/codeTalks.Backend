using System.Linq.Expressions;
using codeTalks.Application.Features.Channels.Dtos;
using codeTalks.Application.Features.Channels.Queries.GetUsersDetailAtChannelByChannelId;
using codeTalks.Application.Services;
using codeTalks.Application.Services.Repositories;
using codeTalks.Domain;
using Core.CrossCuttingConcerns.Exceptions;
using Core.Persistence.Paging;
using Core.Security.Entities;
using FluentAssertions;
using Microsoft.EntityFrameworkCore.Query;
using NSubstitute;

namespace codeTalks.Application.Tests.Features.Channels.Queries.GetUsersDetailAtChannelByChannelId;

// Lists channel users, gated by the caller's per-channel role:
//   - regular members (role "User") may not view Banned/RequestSent lists
//   - for Accepted, admins are fetched separately and excluded from the members list
//   - for Banned, a Moderator sees only banned regular members (exclude Owner+Moderator),
//     while the Owner sees all banned users (no exclusion)
public class GetUsersByChannelIdQueryHandlerTests
{
    private const string ChannelId = "channel-1";
    private const string CurrentUserId = "current-user";

    private static readonly Role OwnerRole = new() { Id = "role-owner", Name = "Owner" };
    private static readonly Role ModeratorRole = new() { Id = "role-mod", Name = "Moderator" };
    private static readonly Role UserRole = new() { Id = "role-user", Name = "User" };

    private readonly IChannelRepository _channelRepository = Substitute.For<IChannelRepository>();
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();
    private readonly GetUsersByChannelIdQuery.GetUsersByChannelIdQueryHandler _handler;

    public GetUsersByChannelIdQueryHandlerTests()
    {
        _currentUserService.GetCurrentUserIdAsync().Returns(CurrentUserId);
        // default: an empty members page so the handler can build its result without NRE
        _channelRepository.GetChannelUsersAsync(
                Arg.Any<Expression<Func<ChannelUser, UsersAtChannelDto>>>(),
                Arg.Any<string>(), Arg.Any<ChannelUserStatus>(),
                Arg.Any<IReadOnlyCollection<string>?>(), Arg.Any<string?>(),
                Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new Paginate<UsersAtChannelDto> { Items = new List<UsersAtChannelDto>() });
        _handler = new GetUsersByChannelIdQuery.GetUsersByChannelIdQueryHandler(_channelRepository, _currentUserService);
    }

    private void SetupCurrentUserRole(Role role)
    {
        var channel = new Channel
        {
            Id = ChannelId,
            ChannelUsers = new List<ChannelUser>
            {
                new() { UserId = CurrentUserId, Status = ChannelUserStatus.Accepted, Role = role, RoleId = role.Id }
            }
        };
        _channelRepository.GetDetailedAsync(
                Arg.Any<Expression<Func<Channel, bool>>>(),
                Arg.Any<Func<IQueryable<Channel>, IIncludableQueryable<Channel, object>>>(),
                Arg.Any<bool>(),
                Arg.Any<CancellationToken>())
            .Returns(channel);
    }

    private static GetUsersByChannelIdQuery Query(ChannelUserStatus status) =>
        new() { ChannelId = ChannelId, Status = status };

    [Theory]
    [InlineData(ChannelUserStatus.Banned)]
    [InlineData(ChannelUserStatus.RequestSent)]
    public async Task Handle_WhenRegularUserRequestsRestrictedStatus_ThrowsAuthorization(ChannelUserStatus status)
    {
        SetupCurrentUserRole(UserRole);

        var act = () => _handler.Handle(Query(status), CancellationToken.None);

        await act.Should().ThrowAsync<AuthorizationException>().WithMessage("*not authorized to view these users*");
    }

    [Fact]
    public async Task Handle_WhenStatusAccepted_FetchesAdminsAndExcludesThemFromMembers()
    {
        SetupCurrentUserRole(OwnerRole);
        var admins = new List<UsersAtChannelDto> { new() { Id = "admin-1" } };
        _channelRepository.GetChannelAdminsAsync(
                Arg.Any<Expression<Func<ChannelUser, UsersAtChannelDto>>>(),
                Arg.Any<string>(), ChannelUserStatus.Accepted, Arg.Any<CancellationToken>())
            .Returns(admins);

        var result = await _handler.Handle(Query(ChannelUserStatus.Accepted), CancellationToken.None);

        result.Admins.Should().BeSameAs(admins);
        await _channelRepository.Received(1).GetChannelUsersAsync(
            Arg.Any<Expression<Func<ChannelUser, UsersAtChannelDto>>>(),
            ChannelId, ChannelUserStatus.Accepted,
            Arg.Is<IReadOnlyCollection<string>?>(x => x != null && x.SequenceEqual(new[] { "Owner", "Moderator" })),
            Arg.Any<string?>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenBannedAndModerator_ExcludesOwnerAndModeratorAndSkipsAdmins()
    {
        SetupCurrentUserRole(ModeratorRole);

        await _handler.Handle(Query(ChannelUserStatus.Banned), CancellationToken.None);

        await _channelRepository.DidNotReceive().GetChannelAdminsAsync(
            Arg.Any<Expression<Func<ChannelUser, UsersAtChannelDto>>>(),
            Arg.Any<string>(), Arg.Any<ChannelUserStatus>(), Arg.Any<CancellationToken>());
        await _channelRepository.Received(1).GetChannelUsersAsync(
            Arg.Any<Expression<Func<ChannelUser, UsersAtChannelDto>>>(),
            ChannelId, ChannelUserStatus.Banned,
            Arg.Is<IReadOnlyCollection<string>?>(x => x != null && x.SequenceEqual(new[] { "Owner", "Moderator" })),
            Arg.Any<string?>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenBannedAndOwner_DoesNotExcludeAnyRoles()
    {
        SetupCurrentUserRole(OwnerRole);

        await _handler.Handle(Query(ChannelUserStatus.Banned), CancellationToken.None);

        await _channelRepository.Received(1).GetChannelUsersAsync(
            Arg.Any<Expression<Func<ChannelUser, UsersAtChannelDto>>>(),
            ChannelId, ChannelUserStatus.Banned,
            Arg.Is<IReadOnlyCollection<string>?>(x => x == null),
            Arg.Any<string?>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenChannelDoesNotExist_ThrowsEntityNotFound()
    {
        _channelRepository.GetDetailedAsync(
                Arg.Any<Expression<Func<Channel, bool>>>(),
                Arg.Any<Func<IQueryable<Channel>, IIncludableQueryable<Channel, object>>>(),
                Arg.Any<bool>(),
                Arg.Any<CancellationToken>())
            .Returns((Channel?)null);

        var act = () => _handler.Handle(Query(ChannelUserStatus.Accepted), CancellationToken.None);

        await act.Should().ThrowAsync<EntityNotFoundException>().WithMessage("*channel doesn't exist*");
    }

    [Fact]
    public async Task Handle_WhenCurrentUserIsNotAnAcceptedMember_ThrowsBusiness()
    {
        var channel = new Channel
        {
            Id = ChannelId,
            ChannelUsers = new List<ChannelUser>
            {
                new() { UserId = "someone-else", Status = ChannelUserStatus.Accepted, Role = OwnerRole, RoleId = OwnerRole.Id }
            }
        };
        _channelRepository.GetDetailedAsync(
                Arg.Any<Expression<Func<Channel, bool>>>(),
                Arg.Any<Func<IQueryable<Channel>, IIncludableQueryable<Channel, object>>>(),
                Arg.Any<bool>(),
                Arg.Any<CancellationToken>())
            .Returns(channel);

        var act = () => _handler.Handle(Query(ChannelUserStatus.Accepted), CancellationToken.None);

        await act.Should().ThrowAsync<BusinessException>().WithMessage("*not authorized to see this channel*");
    }
}