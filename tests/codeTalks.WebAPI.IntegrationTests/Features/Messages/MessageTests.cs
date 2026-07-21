using System.Net;
using System.Net.Http.Json;
using codeTalks.Application.Features.Messages.Commands.CreateMessage;
using codeTalks.Application.Features.Messages.Models;
using codeTalks.Domain;
using codeTalks.Persistence.Contexts;
using codeTalks.WebAPI.IntegrationTests.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace codeTalks.WebAPI.IntegrationTests.Features.Messages;

/// <summary>
/// Full-pipeline coverage of /api/messages: creating a message (persisted + published via the
/// faked <c>IMessagePublisher</c>) and paging a channel's messages. These endpoints carry no
/// <c>[Authorize]</c> attribute — create still requires identity (the handler resolves the
/// current user and 403s without a token), while listing is fully open.
/// </summary>
public sealed class MessageTests(CustomWebApplicationFactory factory) : IntegrationTestBase(factory)
{
    // ---- create --------------------------------------------------------------

    [Fact]
    public async Task Create_message_persists_it_and_returns_200()
    {
        var (author, client) = await CreateUserAsync();
        var channel = await CreateChannelAsync(client);

        var response = await client.PostAsJsonAsync("/api/messages", new CreateMessageCommand
        {
            Content = "hello world",
            ChannelId = channel.Id
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        await using var scope = CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var message = await db.Set<Message>().AsNoTracking()
            .SingleOrDefaultAsync(m => m.ChannelId == channel.Id);
        message.Should().NotBeNull();
        message!.Content.Should().Be("hello world");
        message.SenderId.Should().Be(author.UserId);
    }

    [Fact]
    public async Task Create_message_without_a_token_returns_403()
    {
        // No [Authorize], so the request reaches the handler, which fails to resolve identity.
        var (_, client) = await CreateUserAsync();
        var channel = await CreateChannelAsync(client);

        var response = await Client.PostAsJsonAsync("/api/messages", new CreateMessageCommand
        {
            Content = "no token",
            ChannelId = channel.Id
        });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Create_message_for_unknown_channel_returns_404()
    {
        var (_, client) = await CreateUserAsync();

        var response = await client.PostAsJsonAsync("/api/messages", new CreateMessageCommand
        {
            Content = "into the void",
            ChannelId = Guid.NewGuid().ToString()
        });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Create_message_with_empty_content_returns_400()
    {
        var (_, client) = await CreateUserAsync();
        var channel = await CreateChannelAsync(client);

        var response = await client.PostAsJsonAsync("/api/messages", new CreateMessageCommand
        {
            Content = "",
            ChannelId = channel.Id
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ---- list ----------------------------------------------------------------

    [Fact]
    public async Task Get_messages_returns_them_oldest_first_with_sender()
    {
        var (author, client) = await CreateUserAsync();
        var channel = await CreateChannelAsync(client);

        await PostMessageAsync(client, channel.Id, "first");
        await PostMessageAsync(client, channel.Id, "second");
        await PostMessageAsync(client, channel.Id, "third");

        var response = await client.GetAsync($"/api/messages?channelId={channel.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var page = (await response.Content.ReadFromJsonAsync<MessagesByChannelIdListModel>(JsonWebOptions))!;

        page.Count.Should().Be(3);
        page.Items.Select(m => m.Content).Should().ContainInOrder("first", "second", "third");
        page.Items[0].Sender.Id.Should().Be(author.UserId);
        page.Items[0].Sender.UserName.Should().Be(author.UserName);
    }

    [Fact]
    public async Task Get_messages_respects_page_size()
    {
        var (_, client) = await CreateUserAsync();
        var channel = await CreateChannelAsync(client);

        await PostMessageAsync(client, channel.Id, "m1");
        await PostMessageAsync(client, channel.Id, "m2");
        await PostMessageAsync(client, channel.Id, "m3");

        var response = await client.GetAsync($"/api/messages?channelId={channel.Id}&size=2&index=0");

        var page = (await response.Content.ReadFromJsonAsync<MessagesByChannelIdListModel>(JsonWebOptions))!;
        page.Items.Should().HaveCount(2);
        page.Count.Should().Be(3);
        page.Size.Should().Be(2);
        page.Pages.Should().Be(2);
    }

    [Fact]
    public async Task Get_messages_for_channel_without_messages_returns_empty_page()
    {
        var (_, client) = await CreateUserAsync();
        var channel = await CreateChannelAsync(client);

        var response = await client.GetAsync($"/api/messages?channelId={channel.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var page = (await response.Content.ReadFromJsonAsync<MessagesByChannelIdListModel>(JsonWebOptions))!;
        page.Items.Should().BeEmpty();
        page.Count.Should().Be(0);
    }

    // ---- helpers -------------------------------------------------------------

    private static async Task PostMessageAsync(HttpClient client, string channelId, string content)
    {
        var response = await client.PostAsJsonAsync("/api/messages", new CreateMessageCommand
        {
            Content = content,
            ChannelId = channelId
        });
        response.EnsureSuccessStatusCode();
    }
}