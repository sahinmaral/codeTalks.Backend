using System.Net;
using System.Net.Http.Json;
using codeTalks.Application.Features.Channels.Commands.ChangeUserRole;
using codeTalks.Application.Features.Channels.Commands.CreateChannel;
using codeTalks.Application.Features.Channels.Commands.JoinChannel;
using codeTalks.Application.Features.Channels.Dtos;
using codeTalks.Application.Features.Channels.Models;
using codeTalks.Domain;
using codeTalks.Persistence.Contexts;
using codeTalks.WebAPI.IntegrationTests.Infrastructure;
using Core.Security.Entities;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace codeTalks.WebAPI.IntegrationTests.Features.Channels;

/// <summary>
/// Full-pipeline coverage of /api/channels: creation + owner seeding, GetById authorization,
/// join policies, per-channel role changes, member removal, and leaving. Exercises the
/// per-channel role model and the AuthorizationException→403 / BusinessException→400 /
/// EntityNotFoundException→404 mapping.
/// </summary>
public sealed class ChannelTests(CustomWebApplicationFactory factory) : IntegrationTestBase(factory)
{
    // ---- auth gate + creation ------------------------------------------------

    [Fact]
    public async Task Listing_channels_without_a_token_returns_401()
    {
        var response = await Client.GetAsync("/api/channels");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Create_channel_seeds_creator_as_accepted_owner()
    {
        var (owner, client) = await CreateUserAsync();

        var channel = await CreateChannelAsync(client);

        await using var scope = CreateScope();
        var membership = await GetMembershipAsync(scope, channel.Id, owner.UserId);
        membership.Should().NotBeNull();
        membership!.Status.Should().Be(ChannelUserStatus.Accepted);
        membership.RoleId.Should().Be(await RoleIdAsync(scope, "Owner"));
    }

    [Fact]
    public async Task Create_channel_with_empty_name_returns_400()
    {
        var (_, client) = await CreateUserAsync();

        var response = await client.PostAsJsonAsync("/api/channels", new CreateChannelCommand
        {
            Name = "",
            Description = "desc",
            JoinPolicy = ChannelJoinPolicy.Open
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ---- GetById -------------------------------------------------------------

    [Fact]
    public async Task GetById_as_member_returns_channel_with_role_and_member_count()
    {
        var (_, client) = await CreateUserAsync();
        var channel = await CreateChannelAsync(client);

        var response = await client.GetAsync($"/api/channels/{channel.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var dto = (await response.Content.ReadFromJsonAsync<ChannelByIdDto>(JsonWebOptions))!;
        dto.Id.Should().Be(channel.Id);
        dto.InviteCode.Should().Be(channel.InviteCode);
        dto.MemberCount.Should().Be(1);
        dto.Role.Name.Should().Be("Owner");
    }

    [Fact]
    public async Task GetById_as_non_member_returns_400()
    {
        var (_, ownerClient) = await CreateUserAsync();
        var channel = await CreateChannelAsync(ownerClient);

        var (_, outsiderClient) = await CreateUserAsync();
        var response = await outsiderClient.GetAsync($"/api/channels/{channel.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetById_for_unknown_channel_returns_404()
    {
        var (_, client) = await CreateUserAsync();

        var response = await client.GetAsync($"/api/channels/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ---- join ----------------------------------------------------------------

    [Fact]
    public async Task Join_open_channel_accepts_member_immediately()
    {
        var (_, ownerClient) = await CreateUserAsync();
        var channel = await CreateChannelAsync(ownerClient, ChannelJoinPolicy.Open);

        var (_, joinerClient) = await CreateUserAsync();
        var join = await joinerClient.PostAsJsonAsync("/api/channels/join", new JoinChannelCommand
        {
            InviteCode = channel.InviteCode
        });

        join.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Now a member, so GetById succeeds and reports the joiner's per-channel role.
        var getById = await joinerClient.GetAsync($"/api/channels/{channel.Id}");
        var dto = (await getById.Content.ReadFromJsonAsync<ChannelByIdDto>(JsonWebOptions))!;
        dto.MemberCount.Should().Be(2);
        dto.Role.Name.Should().Be("User");
    }

    [Fact]
    public async Task Join_request_policy_channel_records_request_sent()
    {
        var (_, ownerClient) = await CreateUserAsync();
        var channel = await CreateChannelAsync(ownerClient, ChannelJoinPolicy.Request);

        var (joiner, joinerClient) = await CreateUserAsync();
        var join = await joinerClient.PostAsJsonAsync("/api/channels/join", new JoinChannelCommand
        {
            InviteCode = channel.InviteCode
        });

        join.StatusCode.Should().Be(HttpStatusCode.NoContent);

        await using var scope = CreateScope();
        var membership = await GetMembershipAsync(scope, channel.Id, joiner.UserId);
        membership!.Status.Should().Be(ChannelUserStatus.RequestSent);
    }

    [Fact]
    public async Task Join_with_unknown_invite_code_returns_404()
    {
        var (_, client) = await CreateUserAsync();

        var response = await client.PostAsJsonAsync("/api/channels/join", new JoinChannelCommand
        {
            InviteCode = "does-not-exist"
        });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Join_channel_already_joined_returns_400()
    {
        var (_, ownerClient) = await CreateUserAsync();
        var channel = await CreateChannelAsync(ownerClient);

        // The owner is already an accepted member.
        var response = await ownerClient.PostAsJsonAsync("/api/channels/join", new JoinChannelCommand
        {
            InviteCode = channel.InviteCode
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ---- roles ---------------------------------------------------------------

    [Fact]
    public async Task ChangeUserRole_by_owner_promotes_member()
    {
        var (_, ownerClient) = await CreateUserAsync();
        var channel = await CreateChannelAsync(ownerClient);
        var member = await JoinAsync(channel);

        var response = await ownerClient.PatchAsJsonAsync(
            $"/api/channels/{channel.Id}/users/{member.User.UserId}/role",
            new ChangeUserRoleDto { Role = "Moderator" });

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        await using var scope = CreateScope();
        var membership = await GetMembershipAsync(scope, channel.Id, member.User.UserId);
        membership!.RoleId.Should().Be(await RoleIdAsync(scope, "Moderator"));
    }

    [Fact]
    public async Task ChangeUserRole_by_non_owner_returns_403()
    {
        var (_, ownerClient) = await CreateUserAsync();
        var channel = await CreateChannelAsync(ownerClient);
        var member = await JoinAsync(channel);

        // A regular member tries to change the owner's role.
        var ownerId = await OwnerOf(channel);
        var response = await member.Client.PatchAsJsonAsync(
            $"/api/channels/{channel.Id}/users/{ownerId}/role",
            new ChangeUserRoleDto { Role = "Moderator" });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ---- remove member -------------------------------------------------------

    [Fact]
    public async Task RemoveMember_by_owner_removes_the_member()
    {
        var (_, ownerClient) = await CreateUserAsync();
        var channel = await CreateChannelAsync(ownerClient);
        var member = await JoinAsync(channel);

        var response = await ownerClient.DeleteAsync($"/api/channels/{channel.Id}/users/{member.User.UserId}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        await using var scope = CreateScope();
        (await GetMembershipAsync(scope, channel.Id, member.User.UserId)).Should().BeNull();
    }

    [Fact]
    public async Task RemoveMember_removing_yourself_returns_403()
    {
        var (owner, ownerClient) = await CreateUserAsync();
        var channel = await CreateChannelAsync(ownerClient);

        var response = await ownerClient.DeleteAsync($"/api/channels/{channel.Id}/users/{owner.UserId}");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ---- leave ---------------------------------------------------------------

    [Fact]
    public async Task Leave_channel_as_sole_owner_soft_deletes_it()
    {
        var (_, ownerClient) = await CreateUserAsync();
        var channel = await CreateChannelAsync(ownerClient);

        var response = await ownerClient.PostAsync($"/api/channels/leave/{channel.Id}", null);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        await using var scope = CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        // A global query filter (c => c.IsActive) hides soft-deleted channels, so ignore it here.
        var entity = await db.Set<Channel>().IgnoreQueryFilters().AsNoTracking().SingleAsync(c => c.Id == channel.Id);
        entity.IsActive.Should().BeFalse();
        entity.DeletedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task Leave_channel_as_owner_with_other_members_returns_403()
    {
        var (_, ownerClient) = await CreateUserAsync();
        var channel = await CreateChannelAsync(ownerClient);
        await JoinAsync(channel);

        var response = await ownerClient.PostAsync($"/api/channels/leave/{channel.Id}", null);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Leave_channel_you_never_joined_returns_404()
    {
        var (_, ownerClient) = await CreateUserAsync();
        var channel = await CreateChannelAsync(ownerClient);

        var (_, outsiderClient) = await CreateUserAsync();
        var response = await outsiderClient.PostAsync($"/api/channels/leave/{channel.Id}", null);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ---- helpers -------------------------------------------------------------

    /// <summary>Registers a new user and joins them (Accepted, via Open policy) to the channel.</summary>
    private async Task<(AuthenticatedUser User, HttpClient Client)> JoinAsync(ChannelInfo channel)
    {
        var (user, client) = await CreateUserAsync();
        var join = await client.PostAsJsonAsync("/api/channels/join", new JoinChannelCommand
        {
            InviteCode = channel.InviteCode
        });
        join.EnsureSuccessStatusCode();
        return (user, client);
    }

    private async Task<string> OwnerOf(ChannelInfo channel)
    {
        await using var scope = CreateScope();
        var ownerRoleId = await RoleIdAsync(scope, "Owner");
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var owner = await db.Set<ChannelUser>().AsNoTracking()
            .SingleAsync(cu => cu.ChannelId == channel.Id && cu.RoleId == ownerRoleId);
        return owner.UserId;
    }

    private static async Task<ChannelUser?> GetMembershipAsync(AsyncServiceScope scope, string channelId, string userId)
    {
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await db.Set<ChannelUser>().AsNoTracking()
            .SingleOrDefaultAsync(cu => cu.ChannelId == channelId && cu.UserId == userId);
    }

    private static async Task<string> RoleIdAsync(AsyncServiceScope scope, string roleName)
    {
        var roles = scope.ServiceProvider.GetRequiredService<RoleManager<Role>>();
        return (await roles.FindByNameAsync(roleName))!.Id;
    }
}