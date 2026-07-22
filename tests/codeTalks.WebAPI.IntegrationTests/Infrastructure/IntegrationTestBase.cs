using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using codeTalks.Application.Features.Auths.Commands.LoginUser;
using codeTalks.Application.Features.Auths.Commands.RegisterUser;
using codeTalks.Application.Features.Auths.Dtos;
using codeTalks.Application.Features.Channels.Commands.CreateChannel;
using codeTalks.Domain;
using codeTalks.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace codeTalks.WebAPI.IntegrationTests.Infrastructure;

/// <summary>
/// Base class for integration tests. Provides an <see cref="HttpClient"/> against the
/// shared host, JSON options matching the API's serializer, a DI-scope helper for
/// arranging/asserting straight against the container-backed services, and reusable
/// auth helpers (register + login) that every non-Auth slice builds on.
/// </summary>
[Collection(IntegrationTestCollection.Name)]
public abstract class IntegrationTestBase
{
    protected static readonly JsonSerializerOptions JsonWebOptions = new(JsonSerializerDefaults.Web);

    protected CustomWebApplicationFactory Factory { get; }
    protected HttpClient Client { get; }

    protected IntegrationTestBase(CustomWebApplicationFactory factory)
    {
        Factory = factory;
        Client = factory.CreateClient();
    }

    /// <summary>Opens a service scope backed by the running host's provider.</summary>
    protected AsyncServiceScope CreateScope() => Factory.Services.CreateAsyncScope();

    /// <summary>Registers a fresh unique user and returns the register command + its response.</summary>
    protected async Task<(RegisterUserCommand Command, HttpResponseMessage Response)> RegisterAsync()
    {
        var command = TestUsers.New();
        var response = await Client.PostAsJsonAsync("/api/auth/register", command);
        return (command, response);
    }

    /// <summary>Registers a fresh unique user, logs in, and returns their credentials + tokens.</summary>
    protected async Task<AuthenticatedUser> RegisterAndLoginAsync()
    {
        var command = TestUsers.New();

        var registerResponse = await Client.PostAsJsonAsync("/api/auth/register", command);
        registerResponse.EnsureSuccessStatusCode();

        var loginResponse = await Client.PostAsJsonAsync("/api/auth/login", new LoginUserCommand
        {
            UsernameOrEmail = command.UserName,
            Password = command.Password
        });
        loginResponse.EnsureSuccessStatusCode();

        var dto = (await loginResponse.Content.ReadFromJsonAsync<LoggedUserDto>(JsonWebOptions))!;
        return new AuthenticatedUser(command.UserName, command.Email, command.Password, dto);
    }

    /// <summary>A client whose requests carry the given user's bearer token.</summary>
    protected HttpClient CreateAuthenticatedClient(string accessToken)
    {
        var client = Factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return client;
    }

    /// <summary>Registers + logs in a fresh user and returns them alongside a bearer-token client.</summary>
    protected async Task<(AuthenticatedUser User, HttpClient Client)> CreateUserAsync()
    {
        var user = await RegisterAndLoginAsync();
        return (user, CreateAuthenticatedClient(user.AccessToken));
    }

    /// <summary>
    /// Creates a channel through the API as the given (authenticated) client and returns its
    /// id + invite code. Create returns 204 with no body, so the row is read back from the DB
    /// by its unique name.
    /// </summary>
    protected async Task<ChannelInfo> CreateChannelAsync(
        HttpClient client,
        ChannelJoinPolicy joinPolicy = ChannelJoinPolicy.Open,
        string? name = null)
    {
        name ??= $"ch-{Guid.NewGuid():N}";

        var response = await client.PostAsJsonAsync("/api/channels", new CreateChannelCommand
        {
            Name = name,
            Description = "integration test channel",
            JoinPolicy = joinPolicy
        });
        response.EnsureSuccessStatusCode();

        await using var scope = CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var channel = await db.Set<Channel>().AsNoTracking().SingleAsync(c => c.Name == name);
        return new ChannelInfo(channel.Id, channel.InviteCode, name);
    }

    /// <summary>
    /// Polls <paramref name="condition"/> until it returns true or <paramref name="timeout"/>
    /// elapses. Needed for assertions against work that completes asynchronously off the
    /// request thread (e.g. RabbitMQ publish -> background worker -> Redis write) instead of
    /// synchronously within the HTTP response.
    /// </summary>
    protected static async Task WaitUntilAsync(
        Func<Task<bool>> condition, TimeSpan? timeout = null, TimeSpan? interval = null)
    {
        var deadline = DateTime.UtcNow + (timeout ?? TimeSpan.FromSeconds(5));
        var delay = interval ?? TimeSpan.FromMilliseconds(100);

        while (DateTime.UtcNow < deadline)
        {
            if (await condition())
                return;

            await Task.Delay(delay);
        }

        throw new TimeoutException($"Condition was not met within {timeout ?? TimeSpan.FromSeconds(5)}.");
    }
}

/// <summary>Identifiers for a channel created during a test.</summary>
public sealed record ChannelInfo(string Id, string InviteCode, string Name);

/// <summary>A registered+logged-in user: their credentials and the tokens login returned.</summary>
public sealed record AuthenticatedUser(string UserName, string Email, string Password, LoggedUserDto Login)
{
    public string AccessToken => Login.AccessToken;
    public string RefreshToken => Login.RefreshToken;
    public string UserId => Login.Id;
}