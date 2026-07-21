using codeTalks.Application.Services.FileStorage;
using codeTalks.Application.Services.Notifications.Interfaces;
using codeTalks.Infrastructure.Messaging;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using NSubstitute;
using StackExchange.Redis;
using Testcontainers.PostgreSql;

namespace codeTalks.WebAPI.IntegrationTests.Infrastructure;

/// <summary>
/// Boots the real WebAPI host against a throwaway Postgres container, replacing the
/// external infrastructure the app talks to at startup (Redis, RabbitMQ, Cloudinary)
/// with in-memory fakes. Postgres is real so EF Core, Identity and the migrations that
/// <c>Program</c> applies on boot are all exercised for real.
/// </summary>
public sealed class CustomWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _dbContainer = new PostgreSqlBuilder()
        .WithImage("postgres:16")
        .WithDatabase("codeTalksTestDb")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Development so appsettings.Development.json loads (JwtOptions, etc.). The base
        // appsettings.json carries no JWT config, which AddSecurityServices requires.
        builder.UseEnvironment("Development");

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:PostgreSQLConnectionString"] = _dbContainer.GetConnectionString(),
                // AddInfrastructureService calls ConnectionMultiplexer.Connect eagerly at
                // registration time; abortConnect=false keeps that from throwing when no
                // Redis is reachable. The multiplexer itself is replaced below anyway.
                ["Redis:ConnectionString"] = "localhost:6379,abortConnect=false,connectTimeout=250",
            });
        });

        builder.ConfigureTestServices(services =>
        {
            // Redis: swap the eagerly-connected multiplexer for a stub.
            services.RemoveAll<IConnectionMultiplexer>();
            services.AddSingleton(_ => Substitute.For<IConnectionMultiplexer>());

            // RabbitMQ: drop the background fan-out worker and the connecting publisher.
            RemoveHostedService<ChannelMessageFanoutWorker>(services);
            services.RemoveAll<IMessagePublisher>();
            services.AddSingleton<IMessagePublisher, NoOpMessagePublisher>();

            // Cloudinary: no real uploads in tests.
            services.RemoveAll<ICloudinaryService>();
            services.AddSingleton(_ => Substitute.For<ICloudinaryService>());

            // User-settings cache is Redis-backed; back it with a no-op so settings handlers
            // don't depend on the stubbed multiplexer. Postgres stays the source of truth.
            services.RemoveAll<IUserSettingsCache>();
            services.AddScoped<IUserSettingsCache, NoOpUserSettingsCache>();

            // Unread tracker is Redis-backed; use an in-memory singleton so notification-count
            // tests can seed counts and read them back deterministically.
            services.RemoveAll<IUnreadTracker>();
            services.AddSingleton<IUnreadTracker, FakeUnreadTracker>();
        });
    }

    private static void RemoveHostedService<T>(IServiceCollection services) where T : IHostedService
    {
        var descriptor = services.SingleOrDefault(
            d => d.ServiceType == typeof(IHostedService) && d.ImplementationType == typeof(T));
        if (descriptor is not null)
            services.Remove(descriptor);
    }

    Task IAsyncLifetime.InitializeAsync() => _dbContainer.StartAsync();

    // Explicit impl: WebApplicationFactory already exposes ValueTask DisposeAsync via
    // IAsyncDisposable. Route xUnit's teardown through here and dispose both the host
    // (base) and the container.
    async Task IAsyncLifetime.DisposeAsync()
    {
        await _dbContainer.DisposeAsync();
        await base.DisposeAsync();
    }
}