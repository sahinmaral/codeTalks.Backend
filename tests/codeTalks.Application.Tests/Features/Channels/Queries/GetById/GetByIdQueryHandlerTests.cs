using System.Linq.Expressions;
using codeTalks.Application.Features.Channels.Dtos;
using codeTalks.Application.Features.Channels.Models;
using codeTalks.Application.Features.Channels.Queries.GetById;
using codeTalks.Application.Services;
using codeTalks.Application.Services.Repositories;
using codeTalks.Domain;
using Core.CrossCuttingConcerns.Exceptions;
using Core.Security.Entities;
using FluentAssertions;
using MapsterMapper;
using Microsoft.EntityFrameworkCore.Query;
using NSubstitute;

namespace codeTalks.Application.Tests.Features.Channels.Queries.GetById;

// GetById returns a channel only to an Accepted member (else BusinessException), and
// enriches the mapped DTO with a computed MemberCount (Accepted only) and the caller's role.
public class GetByIdQueryHandlerTests
{
    private const string ChannelId = "channel-1";
    private const string CurrentUserId = "current-user";

    private static readonly Role OwnerRole = new() { Id = "role-owner", Name = "Owner" };

    private readonly IChannelRepository _channelRepository = Substitute.For<IChannelRepository>();
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();
    private readonly IMapper _mapper = Substitute.For<IMapper>();
    private readonly GetByIdQuery.GetByIdQueryHandler _handler;

    public GetByIdQueryHandlerTests()
    {
        _currentUserService.GetCurrentUserIdAsync().Returns(CurrentUserId);
        _handler = new GetByIdQuery.GetByIdQueryHandler(_channelRepository, _currentUserService, _mapper);
    }

    private static ChannelUser Member(string userId, ChannelUserStatus status) =>
        new() { UserId = userId, Status = status, Role = OwnerRole, RoleId = OwnerRole.Id };

    private Channel SetupChannel(params ChannelUser[] members)
    {
        var channel = new Channel { Id = ChannelId, ChannelUsers = members.ToList() };
        _channelRepository.GetDetailedAsync(
                Arg.Any<Expression<Func<Channel, bool>>>(),
                Arg.Any<Func<IQueryable<Channel>, IIncludableQueryable<Channel, object>>>(),
                Arg.Any<bool>(),
                Arg.Any<CancellationToken>())
            .Returns(channel);
        return channel;
    }

    private static GetByIdQuery Query() => new() { ChannelId = ChannelId };

    [Fact]
    public async Task Handle_WhenCurrentUserIsAcceptedMember_ReturnsDtoWithMemberCountAndRole()
    {
        var channel = SetupChannel(
            Member(CurrentUserId, ChannelUserStatus.Accepted),
            Member("u2", ChannelUserStatus.Accepted),
            Member("u3", ChannelUserStatus.Accepted),
            Member("u4", ChannelUserStatus.Banned)); // not counted
        var dto = new ChannelByIdDto();
        var roleDto = new ChannelsByUserIdRoleDto { Id = OwnerRole.Id, Name = "Owner" };
        _mapper.Map<ChannelByIdDto>(channel).Returns(dto);
        _mapper.Map<ChannelsByUserIdRoleDto>(Arg.Any<Role>()).Returns(roleDto);

        var result = await _handler.Handle(Query(), CancellationToken.None);

        result.Should().BeSameAs(dto);
        result.MemberCount.Should().Be(3, "only Accepted members are counted");
        result.Role.Should().BeSameAs(roleDto);
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

        var act = () => _handler.Handle(Query(), CancellationToken.None);

        await act.Should().ThrowAsync<EntityNotFoundException>().WithMessage("*Channel not found*");
    }

    [Fact]
    public async Task Handle_WhenCurrentUserIsNotAnAcceptedMember_ThrowsBusiness()
    {
        SetupChannel(Member(CurrentUserId, ChannelUserStatus.RequestSent)); // pending, not accepted

        var act = () => _handler.Handle(Query(), CancellationToken.None);

        await act.Should().ThrowAsync<BusinessException>().WithMessage("*not authorized to see this channel*");
    }
}