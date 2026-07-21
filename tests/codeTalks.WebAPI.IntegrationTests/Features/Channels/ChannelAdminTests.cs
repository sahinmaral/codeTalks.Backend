using System.Net;
using System.Net.Http.Json;
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
/// Full-pipeline coverage of the owner/admin channel endpoints not in the core-lifecycle
/// suite: the join-request approval workflow (PatchUserStatus), member listing
/// (GetUsersByChannelId), channel update/patch/delete, and the discovery list (GetChannels).
/// </summary>
public sealed class ChannelAdminTests(CustomWebApplicationFactory factory) : IntegrationTestBase(factory)
{
    // ---- join-request approval (PatchUserStatus) -----------------------------

    [Fact]
    public async Task Owner_accepts_a_join_request()
    {
        var (_, ownerClient) = await CreateUserAsync();
        var channel = await CreateChannelAsync(ownerClient, ChannelJoinPolicy.Request);
        var requester = await RequestJoinAsync(channel);

        var response = await ownerClient.PatchAsJsonAsync(
            $"/api/channels/{channel.Id}/users/{requester.UserId}/status",
            new PatchUserStatusDto { Status = ChannelUserStatus.Accepted });

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        await AssertStatusAsync(channel.Id, requester.UserId, ChannelUserStatus.Accepted);
    }

    [Fact]
    public async Task Owner_denies_a_join_request()
    {
        var (_, ownerClient) = await CreateUserAsync();
        var channel = await CreateChannelAsync(ownerClient, ChannelJoinPolicy.Request);
        var requester = await RequestJoinAsync(channel);

        var response = await ownerClient.PatchAsJsonAsync(
            $"/api/channels/{channel.Id}/users/{requester.UserId}/status",
            new PatchUserStatusDto { Status = ChannelUserStatus.Denied });

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        await AssertStatusAsync(channel.Id, requester.UserId, ChannelUserStatus.Denied);
    }

    [Fact]
    public async Task Owner_bans_an_accepted_member()
    {
        var (_, ownerClient) = await CreateUserAsync();
        var channel = await CreateChannelAsync(ownerClient); // Open => joiners are Accepted
        var member = await JoinAsync(channel);

        var response = await ownerClient.PatchAsJsonAsync(
            $"/api/channels/{channel.Id}/users/{member.UserId}/status",
            new PatchUserStatusDto { Status = ChannelUserStatus.Banned });

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        await AssertStatusAsync(channel.Id, member.UserId, ChannelUserStatus.Banned);
    }

    [Fact]
    public async Task Regular_member_cannot_patch_another_members_status_403()
    {
        var (_, ownerClient) = await CreateUserAsync();
        var channel = await CreateChannelAsync(ownerClient);
        var (_, memberClient) = await JoinWithClientAsync(channel);
        var target = await JoinAsync(channel);

        var response = await memberClient.PatchAsJsonAsync(
            $"/api/channels/{channel.Id}/users/{target.UserId}/status",
            new PatchUserStatusDto { Status = ChannelUserStatus.Banned });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Accepting_a_user_on_an_open_channel_returns_400()
    {
        var (_, ownerClient) = await CreateUserAsync();
        var channel = await CreateChannelAsync(ownerClient, ChannelJoinPolicy.Open);
        var member = await JoinAsync(channel);

        var response = await ownerClient.PatchAsJsonAsync(
            $"/api/channels/{channel.Id}/users/{member.UserId}/status",
            new PatchUserStatusDto { Status = ChannelUserStatus.Accepted });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Owner_cannot_patch_their_own_status_403()
    {
        var (owner, ownerClient) = await CreateUserAsync();
        var channel = await CreateChannelAsync(ownerClient, ChannelJoinPolicy.Request);

        var response = await ownerClient.PatchAsJsonAsync(
            $"/api/channels/{channel.Id}/users/{owner.UserId}/status",
            new PatchUserStatusDto { Status = ChannelUserStatus.Banned });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Patching_status_to_request_sent_returns_400()
    {
        var (_, ownerClient) = await CreateUserAsync();
        var channel = await CreateChannelAsync(ownerClient, ChannelJoinPolicy.Request);
        var requester = await RequestJoinAsync(channel);

        // The validator only allows Accepted / Denied / Banned.
        var response = await ownerClient.PatchAsJsonAsync(
            $"/api/channels/{channel.Id}/users/{requester.UserId}/status",
            new PatchUserStatusDto { Status = ChannelUserStatus.RequestSent });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ---- update / patch / delete --------------------------------------------

    [Fact]
    public async Task Owner_updates_name_and_description()
    {
        var (_, ownerClient) = await CreateUserAsync();
        var channel = await CreateChannelAsync(ownerClient);

        var response = await ownerClient.PutAsJsonAsync($"/api/channels/{channel.Id}",
            new UpdateChannelDto { Name = "Renamed", Description = "New description" });

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        await using var scope = CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var entity = await db.Set<Channel>().AsNoTracking().SingleAsync(c => c.Id == channel.Id);
        entity.Name.Should().Be("Renamed");
        entity.Description.Should().Be("New description");
    }

    [Fact]
    public async Task Non_owner_update_returns_403()
    {
        var (_, ownerClient) = await CreateUserAsync();
        var channel = await CreateChannelAsync(ownerClient);
        var (_, memberClient) = await JoinWithClientAsync(channel);

        var response = await memberClient.PutAsJsonAsync($"/api/channels/{channel.Id}",
            new UpdateChannelDto { Name = "Hijacked", Description = "nope" });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Owner_patches_join_policy()
    {
        var (_, ownerClient) = await CreateUserAsync();
        var channel = await CreateChannelAsync(ownerClient, ChannelJoinPolicy.Open);

        var response = await ownerClient.PatchAsJsonAsync($"/api/channels/{channel.Id}",
            new PatchChannelDto { JoinPolicy = ChannelJoinPolicy.Request });

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        await using var scope = CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var entity = await db.Set<Channel>().AsNoTracking().SingleAsync(c => c.Id == channel.Id);
        entity.JoinPolicy.Should().Be(ChannelJoinPolicy.Request);
    }

    [Fact]
    public async Task Owner_deletes_channel_soft_deletes_it()
    {
        var (_, ownerClient) = await CreateUserAsync();
        var channel = await CreateChannelAsync(ownerClient);

        var response = await ownerClient.DeleteAsync($"/api/channels/{channel.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        await using var scope = CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var entity = await db.Set<Channel>().IgnoreQueryFilters().AsNoTracking().SingleAsync(c => c.Id == channel.Id);
        entity.IsActive.Should().BeFalse();
        entity.DeletedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task Non_owner_delete_returns_403()
    {
        var (_, ownerClient) = await CreateUserAsync();
        var channel = await CreateChannelAsync(ownerClient);
        var (_, memberClient) = await JoinWithClientAsync(channel);

        var response = await memberClient.DeleteAsync($"/api/channels/{channel.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ---- discovery list (GetChannels) ---------------------------------------

    [Fact]
    public async Task List_shows_channels_the_caller_has_not_joined()
    {
        // Unique name + title filter isolates this channel from every other test's channels
        // in the shared DB (the discovery list is paged over all non-joined channels).
        var name = $"discover-{Guid.NewGuid():N}";
        var (_, ownerClient) = await CreateUserAsync();
        var channel = await CreateChannelAsync(ownerClient, name: name);

        // A non-member discovers it...
        var (_, outsiderClient) = await CreateUserAsync();
        var outsiderList = await GetChannelsAsync(outsiderClient, title: name);
        outsiderList.Items.Should().ContainSingle().Which.Id.Should().Be(channel.Id);

        // ...but the owner (a member) does not see it in the discovery list.
        var ownerList = await GetChannelsAsync(ownerClient, title: name);
        ownerList.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task List_filters_by_title()
    {
        var unique = $"findme-{Guid.NewGuid():N}";
        var (_, ownerClient) = await CreateUserAsync();
        var channel = await CreateChannelAsync(ownerClient, name: unique);

        var (_, outsiderClient) = await CreateUserAsync();

        var matching = await GetChannelsAsync(outsiderClient, title: unique);
        matching.Items.Should().ContainSingle().Which.Id.Should().Be(channel.Id);

        var nonMatching = await GetChannelsAsync(outsiderClient, title: "no-such-title-xyz");
        nonMatching.Items.Should().NotContain(c => c.Id == channel.Id);
    }

    // ---- member listing (GetUsersByChannelId) --------------------------------

    [Fact]
    public async Task Get_users_lists_members_and_admins_separately()
    {
        var (owner, ownerClient) = await CreateUserAsync();
        var channel = await CreateChannelAsync(ownerClient);
        var member = await JoinAsync(channel);

        var response = await ownerClient.GetAsync($"/api/channels/{channel.Id}/users"); // default status = Accepted

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var page = (await response.Content.ReadFromJsonAsync<UsersAtChannelListModel>(JsonWebOptions))!;
        page.Items.Should().Contain(u => u.Id == member.UserId, "regular members are in Items");
        page.Admins.Should().Contain(u => u.Id == owner.UserId, "the owner is listed under Admins");
    }

    [Fact]
    public async Task Get_users_as_non_member_returns_400()
    {
        var (_, ownerClient) = await CreateUserAsync();
        var channel = await CreateChannelAsync(ownerClient);

        var (_, outsiderClient) = await CreateUserAsync();
        var response = await outsiderClient.GetAsync($"/api/channels/{channel.Id}/users");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ---- helpers -------------------------------------------------------------

    /// <summary>New user joins an Open channel (Accepted); returns the user.</summary>
    private async Task<AuthenticatedUser> JoinAsync(ChannelInfo channel) => (await JoinWithClientAsync(channel)).User;

    private async Task<(AuthenticatedUser User, HttpClient Client)> JoinWithClientAsync(ChannelInfo channel)
    {
        var (user, client) = await CreateUserAsync();
        (await client.PostAsJsonAsync("/api/channels/join", new JoinChannelCommand { InviteCode = channel.InviteCode }))
            .EnsureSuccessStatusCode();
        return (user, client);
    }

    /// <summary>New user requests to join a Request-policy channel (RequestSent); returns the user.</summary>
    private async Task<AuthenticatedUser> RequestJoinAsync(ChannelInfo channel) => await JoinAsync(channel);

    private async Task<ChannelsByUserIdListModel> GetChannelsAsync(HttpClient client, string? title = null)
    {
        var url = "/api/channels?page=1&pageSize=50" + (title is null ? "" : $"&title={Uri.EscapeDataString(title)}");
        var response = await client.GetAsync(url);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<ChannelsByUserIdListModel>(JsonWebOptions))!;
    }

    private async Task AssertStatusAsync(string channelId, string userId, ChannelUserStatus expected)
    {
        await using var scope = CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var membership = await db.Set<ChannelUser>().AsNoTracking()
            .SingleAsync(cu => cu.ChannelId == channelId && cu.UserId == userId);
        membership.Status.Should().Be(expected);
    }
}