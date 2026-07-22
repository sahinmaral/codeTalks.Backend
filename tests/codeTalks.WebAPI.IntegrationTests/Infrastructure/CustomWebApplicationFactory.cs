using codeTalks.Application.Services.FileStorage;
using codeTalks.Application.Services.Notifications.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NSubstitute;
using Testcontainers.PostgreSql;
using Testcontainers.RabbitMq;
using Testcontainers.Redis;

namespace codeTalks.WebAPI.IntegrationTests.Infrastructure;

/// <summary>
/// Boots the real WebAPI host against throwaway Postgres, RabbitMQ, and Redis containers.
/// Postgres, RabbitMQ, and Redis are all real — EF Core, Identity, migrations, message
/// publishing, the background fan-out worker, and unread-count tracking all execute for
/// real. Only actual third-party network calls stay faked: Cloudinary and Expo push.
/// </summary>
public sealed class CustomWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _dbContainer = new PostgreSqlBuilder()
        .WithImage("postgres:16")
        .WithDatabase("codeTalksTestDb")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    // A non-"guest" user is required: RabbitMQ restricts the built-in guest account to
    // loopback-only connections, and a connection through Docker's port mapping doesn't
    // count as loopback from the broker's point of view.
    private readonly RabbitMqContainer _rabbitContainer = new RabbitMqBuilder()
        .WithImage("rabbitmq:3.13-alpine")
        .WithUsername("testuser")
        .WithPassword("testpass")
        .Build();

    private readonly RedisContainer _redisContainer = new RedisBuilder()
        .WithImage("redis:7-alpine")
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
                ["Redis:ConnectionString"] = _redisContainer.GetConnectionString(),
                ["RabbitMq:Host"] = _rabbitContainer.Hostname,
                ["RabbitMq:Port"] = _rabbitContainer.GetMappedPublicPort(5672).ToString(),
                ["RabbitMq:Username"] = "testuser",
                ["RabbitMq:Password"] = "testpass",
            });
        });

        builder.ConfigureTestServices(services =>
        {
            // Cloudinary: no real uploads in tests.
            services.RemoveAll<ICloudinaryService>();
            services.AddSingleton(_ => Substitute.For<ICloudinaryService>());

            // Expo push is a real third-party HTTP call; the fan-out worker now runs for
            // real, so without this it would actually hit https://exp.host in tests.
            services.RemoveAll<IPushNotificationProvider>();
            services.AddSingleton(_ => Substitute.For<IPushNotificationProvider>());

            // User-settings cache still bypasses Redis — orthogonal to the fan-out
            // pipeline; Postgres stays the source of truth for settings.
            services.RemoveAll<IUserSettingsCache>();
            services.AddScoped<IUserSettingsCache, NoOpUserSettingsCache>();
        });
    }

    Task IAsyncLifetime.InitializeAsync() => Task.WhenAll(
        _dbContainer.StartAsync(),
        _rabbitContainer.StartAsync(),
        _redisContainer.StartAsync());

    // Explicit impl: WebApplicationFactory already exposes ValueTask DisposeAsync via
    // IAsyncDisposable. Route xUnit's teardown through here and dispose both the host
    // (base) and the containers.
    async Task IAsyncLifetime.DisposeAsync()
    {
        await Task.WhenAll(
            _dbContainer.DisposeAsync().AsTask(),
            _rabbitContainer.DisposeAsync().AsTask(),
            _redisContainer.DisposeAsync().AsTask());
        await base.DisposeAsync();
    }
}