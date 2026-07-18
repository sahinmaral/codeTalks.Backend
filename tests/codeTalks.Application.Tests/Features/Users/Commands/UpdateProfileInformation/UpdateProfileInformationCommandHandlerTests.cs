using codeTalks.Application.Features.Auths.Rules;
using codeTalks.Application.Features.Users.Commands.UpdateProfileInformation;
using codeTalks.Application.Features.Users.Dtos;
using codeTalks.Application.Services;
using codeTalks.Application.Tests.TestUtilities;
using Core.CrossCuttingConcerns.Exceptions;
using Core.Security.Entities;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using NSubstitute;

namespace codeTalks.Application.Tests.Features.Users.Commands.UpdateProfileInformation;

// Handler tests: the dependencies (current-user service, UserManager) are mocked,
// so we assert on the handler's *behavior* — what it reads, mutates, and persists —
// without a database. AuthBusinessRules is a concrete class with non-virtual methods,
// so we build a real instance over the mocked UserManager rather than mocking the rule.
public class UpdateProfileInformationCommandHandlerTests
{
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();
    private readonly UserManager<User> _userManager = UserManagerMock.Create();
    private readonly UpdateProfileInformationCommand.UpdateProfileInformationCommandCommandHandler _handler;

    public UpdateProfileInformationCommandHandlerTests()
    {
        var authBusinessRules = new AuthBusinessRules(_userManager);
        _handler = new UpdateProfileInformationCommand.UpdateProfileInformationCommandCommandHandler(
            _currentUserService, _userManager, authBusinessRules);
    }

    private static UpdateProfileInformationCommand CommandWith(
        string firstName = "Jane",
        string lastName = "Doe",
        string? middleName = null,
        string? bio = null) =>
        new()
        {
            ProfileInformation = new UpdateProfileInformationDto
            {
                FirstName = firstName,
                LastName = lastName,
                MiddleName = middleName,
                Bio = bio
            }
        };

    [Fact]
    public async Task Handle_WhenUserExists_UpdatesEditableFieldsAndPersists()
    {
        // Arrange
        var existingUser = new User
        {
            FirstName = "Old",
            LastName = "Name",
            MiddleName = "Keep",
            Bio = "Old bio"
        };
        _currentUserService.GetCurrentUserIdAsync().Returns(existingUser.Id);
        _userManager.FindByIdAsync(existingUser.Id).Returns(existingUser);
        _userManager.UpdateAsync(existingUser).Returns(IdentityResult.Success);

        // Act
        await _handler.Handle(CommandWith(firstName: "Jane", lastName: "Doe"), CancellationToken.None);

        // Assert
        existingUser.FirstName.Should().Be("Jane");
        existingUser.LastName.Should().Be("Doe");
        await _userManager.Received(1).UpdateAsync(existingUser);
    }

    [Fact]
    public async Task Handle_WhenOptionalFieldsAreNull_KeepsExistingValues()
    {
        // MiddleName/Bio use "?? existing", so null in the request must not overwrite.
        var existingUser = new User { MiddleName = "Keep", Bio = "Keep bio" };
        _currentUserService.GetCurrentUserIdAsync().Returns(existingUser.Id);
        _userManager.FindByIdAsync(existingUser.Id).Returns(existingUser);
        _userManager.UpdateAsync(existingUser).Returns(IdentityResult.Success);

        await _handler.Handle(CommandWith(middleName: null, bio: null), CancellationToken.None);

        existingUser.MiddleName.Should().Be("Keep");
        existingUser.Bio.Should().Be("Keep bio");
    }

    [Fact]
    public async Task Handle_WhenUserDoesNotExist_ThrowsAndDoesNotPersist()
    {
        _currentUserService.GetCurrentUserIdAsync().Returns("missing-id");
        _userManager.FindByIdAsync("missing-id").Returns((User?)null);

        var act = () => _handler.Handle(CommandWith(), CancellationToken.None);

        await act.Should().ThrowAsync<EntityNotFoundException>();
        await _userManager.DidNotReceive().UpdateAsync(Arg.Any<User>());
    }
}