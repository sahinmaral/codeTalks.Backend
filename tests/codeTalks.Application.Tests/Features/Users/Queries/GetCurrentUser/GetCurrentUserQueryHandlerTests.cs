using System.Linq.Expressions;
using codeTalks.Application.Features.Users.Dtos;
using codeTalks.Application.Features.Users.Queries.GetCurrentUser;
using codeTalks.Application.Services;
using codeTalks.Application.Services.Repositories;
using codeTalks.Application.Tests.TestUtilities;
using codeTalks.Domain;
using Core.CrossCuttingConcerns.Exceptions;
using Core.Persistence.Paging;
using Core.Security.Entities;
using FluentAssertions;
using MapsterMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore.Query;
using NSubstitute;

namespace codeTalks.Application.Tests.Features.Users.Queries.GetCurrentUser;

// Assembles the current user's profile: the mapped user plus a computed JoinedChannelCount
// and mapped status. Missing user or status is (currently) surfaced as InvalidOperationException.
public class GetCurrentUserQueryHandlerTests
{
    private const string CurrentUserId = "current-user";

    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();
    private readonly UserManager<User> _userManager = UserManagerMock.Create();
    private readonly IChannelRepository _channelRepository = Substitute.For<IChannelRepository>();
    private readonly IUserStatusRepository _userStatusRepository = Substitute.For<IUserStatusRepository>();
    private readonly IMapper _mapper = Substitute.For<IMapper>();
    private readonly GetCurrentUserQuery.GetCurrentUserQueryHandler _handler;

    private readonly User _user = new() { Id = CurrentUserId, UserName = "jane" };
    private readonly UserStatus _userStatus = new() { UserId = CurrentUserId, Status = UserStatusType.Online };
    private readonly GetUserStatusDto _statusDto = new() { Status = UserStatusType.Online };
    private readonly GetCurrentUserDto _dto = new();

    public GetCurrentUserQueryHandlerTests()
    {
        _currentUserService.GetCurrentUserIdAsync().Returns(CurrentUserId);
        _userManager.FindByIdAsync(CurrentUserId).Returns(_user);
        _channelRepository.GetListAsync(
                Arg.Any<Expression<Func<Channel, bool>>>(),
                Arg.Any<Func<IQueryable<Channel>, IOrderedQueryable<Channel>>>(),
                Arg.Any<Func<IQueryable<Channel>, IIncludableQueryable<Channel, object>>>(),
                Arg.Any<int>(), Arg.Any<int>(), Arg.Any<bool>(),
                Arg.Any<CancellationToken>())
            .Returns(new Paginate<Channel> { Count = 3 });
        _userStatusRepository.GetAsync(Arg.Any<Expression<Func<UserStatus, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(_userStatus);
        _mapper.Map<GetUserStatusDto>(_userStatus).Returns(_statusDto);
        _mapper.Map<GetCurrentUserDto>(_user).Returns(_dto);

        _handler = new GetCurrentUserQuery.GetCurrentUserQueryHandler(
            _currentUserService, _userManager, _channelRepository, _userStatusRepository, _mapper);
    }

    [Fact]
    public async Task Handle_WhenUserAndStatusExist_ReturnsDtoWithJoinedChannelCountAndStatus()
    {
        var result = await _handler.Handle(new GetCurrentUserQuery(), CancellationToken.None);

        result.Should().BeSameAs(_dto);
        result.JoinedChannelCount.Should().Be(3);
        result.UserStatus.Should().BeSameAs(_statusDto);
    }

    [Fact]
    public async Task Handle_WhenUserNotFound_ThrowsEntityNotFound()
    {
        _userManager.FindByIdAsync(CurrentUserId).Returns((User?)null);

        var act = () => _handler.Handle(new GetCurrentUserQuery(), CancellationToken.None);

        await act.Should().ThrowAsync<EntityNotFoundException>().WithMessage("*User not found*");
    }

    [Fact]
    public async Task Handle_WhenUserStatusNotFound_ThrowsEntityNotFound()
    {
        _userStatusRepository.GetAsync(Arg.Any<Expression<Func<UserStatus, bool>>>(), Arg.Any<CancellationToken>())
            .Returns((UserStatus?)null);

        var act = () => _handler.Handle(new GetCurrentUserQuery(), CancellationToken.None);

        await act.Should().ThrowAsync<EntityNotFoundException>().WithMessage("*status not found*");
    }
}