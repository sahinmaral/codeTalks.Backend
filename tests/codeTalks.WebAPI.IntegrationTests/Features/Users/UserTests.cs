using System.Net;
using System.Net.Http.Json;
using codeTalks.Application.Features.Users.Commands.ChangeUserPassword;
using codeTalks.Application.Features.Users.Commands.UpdateUserNotificationSetting;
using codeTalks.Application.Features.Users.Commands.UpdateUserStatus;
using codeTalks.Application.Features.Users.Dtos;
using codeTalks.Domain;
using codeTalks.Persistence.Contexts;
using codeTalks.WebAPI.IntegrationTests.Infrastructure;
using Core.Security.Entities;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace codeTalks.WebAPI.IntegrationTests.Features.Users;

/// <summary>
/// Full-pipeline coverage of /api/users: the current-user projection, status, password change,
/// profile info, notification settings, and per-channel mute settings. The Redis-backed
/// <c>IUserSettingsCache</c> is faked, so Postgres is the source of truth for every assertion.
/// </summary>
public sealed class UserTests(CustomWebApplicationFactory factory) : IntegrationTestBase(factory)
{
    // ---- GET /me -------------------------------------------------------------

    [Fact]
    public async Task Get_me_without_token_returns_401()
    {
        var response = await Client.GetAsync("/api/users/me");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Get_me_returns_profile_with_status_and_joined_channel_count()
    {
        var (user, client) = await CreateUserAsync();
        await CreateChannelAsync(client); // owner => a joined channel

        var response = await client.GetAsync("/api/users/me");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var dto = (await response.Content.ReadFromJsonAsync<GetCurrentUserDto>(JsonWebOptions))!;
        dto.Id.Should().Be(user.UserId);
        dto.UserName.Should().Be(user.UserName);
        dto.Email.Should().Be(user.Email);
        dto.JoinedChannelCount.Should().Be(1);
        dto.UserStatus.Status.Should().Be(UserStatusType.Online); // seeded at register
    }

    // ---- status --------------------------------------------------------------

    [Fact]
    public async Task Update_status_persists_new_status()
    {
        var (user, client) = await CreateUserAsync();

        var response = await client.PutAsJsonAsync("/api/users/status", new UpdateUserStatusCommand
        {
            Status = UserStatusType.Busy
        });

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        await using var scope = CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var status = await db.Set<UserStatus>().AsNoTracking().SingleAsync(s => s.UserId == user.UserId);
        status.Status.Should().Be(UserStatusType.Busy);
    }

    [Fact]
    public async Task Update_status_with_value_outside_enum_returns_400()
    {
        var (_, client) = await CreateUserAsync();

        var response = await client.PutAsJsonAsync("/api/users/status", new UpdateUserStatusCommand
        {
            Status = (UserStatusType)99
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ---- password ------------------------------------------------------------

    [Fact]
    public async Task Change_password_updates_the_login_credential()
    {
        var (user, client) = await CreateUserAsync();
        const string newPassword = "NewPassw0rd";

        var change = await client.PutAsJsonAsync("/api/users/password", new ChangeUserPasswordCommand
        {
            CurrentPassword = user.Password,
            NewPassword = newPassword
        });
        change.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // New password works, old one no longer does.
        (await LoginAsync(user.UserName, newPassword)).StatusCode.Should().Be(HttpStatusCode.OK);
        (await LoginAsync(user.UserName, user.Password)).StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Change_password_with_wrong_current_returns_400()
    {
        var (_, client) = await CreateUserAsync();

        var response = await client.PutAsJsonAsync("/api/users/password", new ChangeUserPasswordCommand
        {
            CurrentPassword = "not-my-password",
            NewPassword = "NewPassw0rd"
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Change_password_to_same_value_returns_400()
    {
        var (user, client) = await CreateUserAsync();

        var response = await client.PutAsJsonAsync("/api/users/password", new ChangeUserPasswordCommand
        {
            CurrentPassword = user.Password,
            NewPassword = user.Password
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ---- profile info --------------------------------------------------------

    [Fact]
    public async Task Update_profile_without_token_returns_401()
    {
        var response = await Client.PutAsJsonAsync("/api/users/profile", new UpdateProfileInformationDto
        {
            FirstName = "Ada",
            LastName = "Lovelace"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Update_profile_persists_changes()
    {
        var (user, client) = await CreateUserAsync();

        var response = await client.PutAsJsonAsync("/api/users/profile", new UpdateProfileInformationDto
        {
            FirstName = "Ada",
            LastName = "Lovelace",
            Bio = "Countess of Lovelace"
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        await using var scope = CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
        var entity = await users.FindByIdAsync(user.UserId);
        entity!.FirstName.Should().Be("Ada");
        entity.LastName.Should().Be("Lovelace");
        entity.Bio.Should().Be("Countess of Lovelace");
    }

    [Fact]
    public async Task Update_profile_with_too_short_first_name_returns_400()
    {
        var (_, client) = await CreateUserAsync();

        var response = await client.PutAsJsonAsync("/api/users/profile", new UpdateProfileInformationDto
        {
            FirstName = "A",
            LastName = "Lovelace"
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ---- notification settings ----------------------------------------------

    [Fact]
    public async Task Get_notification_settings_returns_seeded_defaults()
    {
        var (_, client) = await CreateUserAsync();

        var response = await client.GetAsync("/api/users/me/notification-settings");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var dto = (await response.Content.ReadFromJsonAsync<UserNotificationSettingDto>(JsonWebOptions))!;
        dto.IsEnabled.Should().BeTrue();
        dto.IsSoundEnabled.Should().BeFalse();
    }

    [Fact]
    public async Task Update_notification_settings_persists_sound_flag()
    {
        var (_, client) = await CreateUserAsync();

        var update = await client.PutAsJsonAsync("/api/users/me/notification-settings",
            new UpdateUserNotificationSettingCommand { IsSoundEnabled = true });
        update.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var dto = (await (await client.GetAsync("/api/users/me/notification-settings"))
            .Content.ReadFromJsonAsync<UserNotificationSettingDto>(JsonWebOptions))!;
        dto.IsSoundEnabled.Should().BeTrue();
        dto.IsEnabled.Should().BeTrue();
    }

    // ---- channel mute settings ----------------------------------------------

    [Fact]
    public async Task Mute_channel_then_list_shows_it_as_muted()
    {
        var (_, client) = await CreateUserAsync();
        var channel = await CreateChannelAsync(client);

        var mute = await client.PutAsJsonAsync($"/api/users/me/channel-mute-settings/{channel.Id}",
            new UpdateChannelMuteSettingDto { MuteUntil = DateTime.UtcNow.AddHours(1) });
        mute.StatusCode.Should().Be(HttpStatusCode.OK);

        var settings = (await (await client.GetAsync("/api/users/me/channel-mute-settings"))
            .Content.ReadFromJsonAsync<List<UserChannelMuteSettingDto>>(JsonWebOptions))!;
        var muted = settings.Should().ContainSingle(s => s.ChannelId == channel.Id).Subject;
        muted.IsMuted.Should().BeTrue();
        muted.MutedUntil.Should().BeAfter(DateTime.UtcNow);
    }

    [Fact]
    public async Task Mute_channel_with_past_date_returns_400()
    {
        var (_, client) = await CreateUserAsync();
        var channel = await CreateChannelAsync(client);

        var response = await client.PutAsJsonAsync($"/api/users/me/channel-mute-settings/{channel.Id}",
            new UpdateChannelMuteSettingDto { MuteUntil = DateTime.UtcNow.AddHours(-1) });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Unmute_channel_removes_the_setting()
    {
        var (_, client) = await CreateUserAsync();
        var channel = await CreateChannelAsync(client);
        (await client.PutAsJsonAsync($"/api/users/me/channel-mute-settings/{channel.Id}",
            new UpdateChannelMuteSettingDto { MuteUntil = DateTime.UtcNow.AddHours(1) })).EnsureSuccessStatusCode();

        var unmute = await client.DeleteAsync($"/api/users/me/channel-mute-settings/{channel.Id}");
        unmute.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var settings = (await (await client.GetAsync("/api/users/me/channel-mute-settings"))
            .Content.ReadFromJsonAsync<List<UserChannelMuteSettingDto>>(JsonWebOptions))!;
        settings.Should().NotContain(s => s.ChannelId == channel.Id);
    }

    [Fact]
    public async Task Unmute_channel_that_was_never_muted_returns_400()
    {
        var (_, client) = await CreateUserAsync();
        var channel = await CreateChannelAsync(client);

        var response = await client.DeleteAsync($"/api/users/me/channel-mute-settings/{channel.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ---- helpers -------------------------------------------------------------

    private Task<HttpResponseMessage> LoginAsync(string usernameOrEmail, string password) =>
        Client.PostAsJsonAsync("/api/auth/login", new { UsernameOrEmail = usernameOrEmail, Password = password });
}