using System.Linq.Expressions;
using codeTalks.Application.Features.Auths.Rules;
using codeTalks.Application.Features.Users.Dtos;
using codeTalks.Application.Features.Users.Queries.GetUserNotificationSettings;
using codeTalks.Application.Services;
using codeTalks.Application.Services.Repositories;
using codeTalks.Application.UnitTests.TestUtilities;
using codeTalks.Domain;
using Core.CrossCuttingConcerns.Exceptions;
using Core.Security.Entities;
using FluentAssertions;
using MapsterMapper;
using Microsoft.AspNetCore.Identity;
using NSubstitute;

namespace codeTalks.Application.UnitTests.Features.Users.Queries.GetUserNotificationSettings;

// Not a thin passthrough: it 404s when the settings row is missing, a branch worth
// covering on its own since AuthBusinessRules.CheckUserExistsById handles the "user
// doesn't exist" case separately.
public class GetUserNotificationSettingsQueryHandlerTests
{
    private const string CurrentUserId = "current-user";

    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();
    private readonly IUserNotificationSettingRepository _settingRepository =
        Substitute.For<IUserNotificationSettingRepository>();
    private readonly IMapper _mapper = Substitute.For<IMapper>();
    private readonly UserManager<User> _userManager = UserManagerMock.Create();
    private readonly GetUserNotificationSettingsQuery.GetUserNotificationSettingsQueryHandler _handler;

    public GetUserNotificationSettingsQueryHandlerTests()
    {
        _currentUserService.GetCurrentUserIdAsync().Returns(CurrentUserId);
        _userManager.FindByIdAsync(CurrentUserId).Returns(new User { Id = CurrentUserId });
        var authBusinessRules = new AuthBusinessRules(_userManager);
        _handler = new GetUserNotificationSettingsQuery.GetUserNotificationSettingsQueryHandler(
            _currentUserService, _settingRepository, _mapper, authBusinessRules);
    }

    [Fact]
    public async Task Handle_WhenSettingsExist_ReturnsMappedDto()
    {
        var setting = new UserNotificationSetting
        {
            UserId = CurrentUserId, IsEnabled = true, IsSoundEnabled = false
        };
        _settingRepository.GetAsync(
                Arg.Any<Expression<Func<UserNotificationSetting, bool>>>(),
                Arg.Any<CancellationToken>())
            .Returns(setting);
        var dto = new UserNotificationSettingDto { IsEnabled = true, IsSoundEnabled = false };
        _mapper.Map<UserNotificationSettingDto>(setting).Returns(dto);

        var result = await _handler.Handle(new GetUserNotificationSettingsQuery(), CancellationToken.None);

        result.Should().BeSameAs(dto);
    }

    [Fact]
    public async Task Handle_WhenSettingsDoNotExist_ThrowsEntityNotFound()
    {
        _settingRepository.GetAsync(
                Arg.Any<Expression<Func<UserNotificationSetting, bool>>>(),
                Arg.Any<CancellationToken>())
            .Returns((UserNotificationSetting?)null);

        var act = () => _handler.Handle(new GetUserNotificationSettingsQuery(), CancellationToken.None);

        await act.Should().ThrowAsync<EntityNotFoundException>().WithMessage("*notification settings does not exist*");
    }

    [Fact]
    public async Task Handle_WhenCurrentUserDoesNotExist_ThrowsEntityNotFound()
    {
        _userManager.FindByIdAsync(CurrentUserId).Returns((User?)null);

        var act = () => _handler.Handle(new GetUserNotificationSettingsQuery(), CancellationToken.None);

        await act.Should().ThrowAsync<EntityNotFoundException>();
    }
}
