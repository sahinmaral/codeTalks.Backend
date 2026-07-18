using codeTalks.Application.Services;
using codeTalks.Application.Services.FileStorage;
using codeTalks.Application.Services.Notifications;
using codeTalks.Application.Services.Notifications.Interfaces;
using codeTalks.Application.Services.Notifications.Models;
using codeTalks.Infrastructure.FileStorage;
using codeTalks.Infrastructure.Messaging;
using codeTalks.Infrastructure.Notifications;
using codeTalks.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace codeTalks.Infrastructure;

public static class InfrastructureServiceRegistration
{
    public static IServiceCollection AddInfrastructureService(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IInviteCodeGenerator, InviteCodeGenerator>();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        
        services.AddSingleton<ICloudinaryService, CloudinaryService>();
        services.ConfigureOptions<CloudinaryOptionsSetup>();
        
        services.AddSingleton<IConnectionMultiplexer>(
            ConnectionMultiplexer.Connect(configuration["Redis:ConnectionString"]!));
        
        services.AddScoped<IConnectionTracker, RedisConnectionTracker>();
        services.AddScoped<IUserSettingsCache, UserSettingsCache>();
        services.AddScoped<IUnreadTracker, RedisUnreadTracker>();
        services.AddScoped<IUserDeliveryContextFactory, UserDeliveryContextFactory>();
        services.AddScoped<IPushNotificationProvider, ExpoPushNotificationProvider>();
        services.AddSingleton<NotificationDecisionEngine>();
        services.AddScoped<ChannelFanoutService>();
        
        services.Configure<RabbitMqOptions>(
            configuration.GetSection("RabbitMq"));
        services.AddSingleton<IMessagePublisher, RabbitMqPublisher>();
        services.AddHostedService<ChannelMessageFanoutWorker>();

        return services;
    }
}