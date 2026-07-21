using System.Net;
using System.Net.Http.Json;
using codeTalks.Application.Features.Devices.Commands;
using codeTalks.Domain;
using codeTalks.Persistence.Contexts;
using codeTalks.WebAPI.IntegrationTests.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace codeTalks.WebAPI.IntegrationTests.Features.Devices;

/// <summary>
/// Full-pipeline coverage of /api/devices (push-token registration). Pure Postgres — persists
/// and removes <see cref="UserDevice"/> rows for the current user; both handlers are idempotent.
/// </summary>
public sealed class DeviceTests(CustomWebApplicationFactory factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task Register_device_without_token_returns_401()
    {
        var response = await Client.PostAsJsonAsync("/api/devices/register", new RegisterDeviceCommand
        {
            DeviceToken = "tok"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Register_device_persists_it()
    {
        var (user, client) = await CreateUserAsync();

        var response = await client.PostAsJsonAsync("/api/devices/register", new RegisterDeviceCommand
        {
            DeviceToken = "device-token-1"
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        await using var scope = CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var devices = await db.Set<UserDevice>().AsNoTracking()
            .Where(d => d.UserId == user.UserId).ToListAsync();
        devices.Should().ContainSingle().Which.DeviceToken.Should().Be("device-token-1");
    }

    [Fact]
    public async Task Register_same_device_twice_is_idempotent()
    {
        var (user, client) = await CreateUserAsync();
        var command = new RegisterDeviceCommand { DeviceToken = "dup-token" };

        (await client.PostAsJsonAsync("/api/devices/register", command)).EnsureSuccessStatusCode();
        (await client.PostAsJsonAsync("/api/devices/register", command)).EnsureSuccessStatusCode();

        await using var scope = CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var count = await db.Set<UserDevice>().CountAsync(d => d.UserId == user.UserId && d.DeviceToken == "dup-token");
        count.Should().Be(1);
    }

    [Fact]
    public async Task Remove_device_deletes_it()
    {
        var (user, client) = await CreateUserAsync();
        (await client.PostAsJsonAsync("/api/devices/register", new RegisterDeviceCommand { DeviceToken = "to-remove" }))
            .EnsureSuccessStatusCode();

        var remove = await SendJsonAsync(client, HttpMethod.Delete, "/api/devices/remove",
            new RemoveDeviceCommand { DeviceToken = "to-remove" });
        remove.StatusCode.Should().Be(HttpStatusCode.OK);

        await using var scope = CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        (await db.Set<UserDevice>().AnyAsync(d => d.UserId == user.UserId && d.DeviceToken == "to-remove"))
            .Should().BeFalse();
    }

    [Fact]
    public async Task Remove_unknown_device_is_a_noop_and_returns_200()
    {
        var (_, client) = await CreateUserAsync();

        var response = await SendJsonAsync(client, HttpMethod.Delete, "/api/devices/remove",
            new RemoveDeviceCommand { DeviceToken = "never-registered" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private static Task<HttpResponseMessage> SendJsonAsync<T>(HttpClient client, HttpMethod method, string url, T body) =>
        client.SendAsync(new HttpRequestMessage(method, url) { Content = JsonContent.Create(body) });
}