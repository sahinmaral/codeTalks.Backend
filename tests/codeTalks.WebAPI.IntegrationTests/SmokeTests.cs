using System.Net;
using codeTalks.Persistence.Contexts;
using codeTalks.WebAPI.IntegrationTests.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace codeTalks.WebAPI.IntegrationTests;

/// <summary>
/// Proves the harness itself: the host boots against the Postgres container with the
/// infrastructure fakes in place, migrations applied, and the auth pipeline wired.
/// </summary>
public sealed class SmokeTests(CustomWebApplicationFactory factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task Protected_endpoint_without_token_returns_401()
    {
        var response = await Client.GetAsync("/api/notifications/unread-count");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Database_is_migrated_and_reachable()
    {
        await using var scope = CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var canConnect = await dbContext.Database.CanConnectAsync();
        var appliedMigrations = await dbContext.Database.GetAppliedMigrationsAsync();

        canConnect.Should().BeTrue();
        appliedMigrations.Should().NotBeEmpty("Program applies EF Core migrations on startup");
    }
}