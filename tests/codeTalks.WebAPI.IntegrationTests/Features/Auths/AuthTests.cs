using System.Net;
using System.Net.Http.Json;
using codeTalks.Application.Features.Auths.Commands.LoginUser;
using codeTalks.Application.Features.Auths.Commands.RefreshToken;
using codeTalks.Application.Features.Auths.Dtos;
using codeTalks.Domain;
using codeTalks.Persistence.Contexts;
using codeTalks.WebAPI.IntegrationTests.Infrastructure;
using Core.Security.Entities;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace codeTalks.WebAPI.IntegrationTests.Features.Auths;

/// <summary>
/// Full-pipeline coverage of /api/auth (register, login, refresh): routing, the
/// FluentValidation behavior, business rules, ASP.NET Identity + JWT, and the
/// exception→status mapping, all against the real Postgres container.
/// </summary>
public sealed class AuthTests(CustomWebApplicationFactory factory) : IntegrationTestBase(factory)
{
    // ---- register ------------------------------------------------------------

    [Fact]
    public async Task Register_with_valid_payload_returns_201_and_persists_user_with_defaults()
    {
        var (command, response) = await RegisterAsync();

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var body = (await response.Content.ReadFromJsonAsync<RegisteredUserDto>(JsonWebOptions))!;
        body.UserName.Should().Be(command.UserName);
        body.Email.Should().Be(command.Email);
        body.Id.Should().NotBeNullOrEmpty();

        // The handler also seeds a UserStatus and a UserNotificationSetting row.
        await using var scope = CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var status = await db.Set<UserStatus>().SingleOrDefaultAsync(x => x.UserId == body.Id);
        status.Should().NotBeNull();
        status!.Status.Should().Be(UserStatusType.Online);

        var notification = await db.Set<UserNotificationSetting>().SingleOrDefaultAsync(x => x.UserId == body.Id);
        notification.Should().NotBeNull();
        notification!.IsEnabled.Should().BeTrue();
    }

    [Fact]
    public async Task Register_with_invalid_email_returns_400()
    {
        var command = TestUsers.New();
        command.Email = "not-an-email";

        var response = await Client.PostAsJsonAsync("/api/auth/register", command);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Register_with_duplicate_username_returns_400()
    {
        var first = TestUsers.New();
        (await Client.PostAsJsonAsync("/api/auth/register", first)).EnsureSuccessStatusCode();

        var duplicate = TestUsers.New();
        duplicate.UserName = first.UserName; // collide on username, unique email

        var response = await Client.PostAsJsonAsync("/api/auth/register", duplicate);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ---- login ---------------------------------------------------------------

    [Fact]
    public async Task Login_with_correct_credentials_returns_200_with_jwt_and_persists_refresh_token()
    {
        var command = TestUsers.New();
        (await Client.PostAsJsonAsync("/api/auth/register", command)).EnsureSuccessStatusCode();

        var response = await Client.PostAsJsonAsync("/api/auth/login", new LoginUserCommand
        {
            UsernameOrEmail = command.UserName,
            Password = command.Password
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var dto = (await response.Content.ReadFromJsonAsync<LoggedUserDto>(JsonWebOptions))!;
        dto.AccessToken.Split('.').Should().HaveCount(3, "the access token is a JWT (header.payload.signature)");
        dto.RefreshToken.Should().NotBeNullOrEmpty();
        dto.RefreshTokenExpires.Should().BeAfter(DateTime.UtcNow);

        // The refresh token JwtProvider issued must be persisted on the user.
        await using var scope = CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
        var user = await users.FindByNameAsync(command.UserName);
        user!.RefreshToken.Should().Be(dto.RefreshToken);
    }

    [Fact]
    public async Task Login_with_wrong_password_returns_400()
    {
        var command = TestUsers.New();
        (await Client.PostAsJsonAsync("/api/auth/register", command)).EnsureSuccessStatusCode();

        var response = await Client.PostAsJsonAsync("/api/auth/login", new LoginUserCommand
        {
            UsernameOrEmail = command.UserName,
            Password = "wrong-password"
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Login_with_unknown_user_returns_404()
    {
        var response = await Client.PostAsJsonAsync("/api/auth/login", new LoginUserCommand
        {
            UsernameOrEmail = "nobody@test.local",
            Password = TestUsers.DefaultPassword
        });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ---- refresh -------------------------------------------------------------

    [Fact]
    public async Task Refresh_with_valid_token_rotates_tokens_and_invalidates_the_old_one()
    {
        var auth = await RegisterAndLoginAsync();

        var response = await Client.PostAsJsonAsync("/api/auth/refresh", new RefreshTokenCommand
        {
            RefreshToken = auth.RefreshToken
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var refreshed = (await response.Content.ReadFromJsonAsync<RefreshedTokenDto>(JsonWebOptions))!;
        refreshed.RefreshToken.Should().NotBeNullOrEmpty().And.NotBe(auth.RefreshToken, "refresh rotates the token");
        refreshed.AccessToken.Split('.').Should().HaveCount(3);

        // The old refresh token no longer matches the persisted one.
        var reuse = await Client.PostAsJsonAsync("/api/auth/refresh", new RefreshTokenCommand
        {
            RefreshToken = auth.RefreshToken
        });
        reuse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Refresh_with_invalid_token_returns_401()
    {
        var response = await Client.PostAsJsonAsync("/api/auth/refresh", new RefreshTokenCommand
        {
            RefreshToken = Guid.NewGuid().ToString()
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Refresh_with_expired_token_returns_401()
    {
        var auth = await RegisterAndLoginAsync();

        // Force the stored refresh token into the past.
        await using (var scope = CreateScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
            var user = await users.FindByIdAsync(auth.UserId);
            user!.RefreshTokenExpires = DateTime.UtcNow.AddDays(-1);
            (await users.UpdateAsync(user)).Succeeded.Should().BeTrue();
        }

        var response = await Client.PostAsJsonAsync("/api/auth/refresh", new RefreshTokenCommand
        {
            RefreshToken = auth.RefreshToken
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}