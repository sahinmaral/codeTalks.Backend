using codeTalks.Application.Services.Repositories;
using codeTalks.Persistence.Contexts;
using codeTalks.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace codeTalks.Persistence;

public static class PersistanceServiceRegistration
{
    public static IServiceCollection AddPersistanceServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IMessageRepository, MessageRepository>();
        services.AddScoped<IChannelRepository, ChannelRepository>();
        services.AddScoped<IUserStatusRepository, UserStatusRepository>();
        services.AddScoped<IUserNotificationSettingRepository, UserNotificationSettingRepository>();
        services.AddScoped<IUserChannelMuteSettingRepository, UserChannelMuteSettingRepository>();
        services.AddScoped<IUserDeviceRepository, UserDeviceRepository>();

        services.AddDbContext<AppDbContext>(options =>
        {
            options.UseNpgsql(configuration.GetConnectionString("PostgreSQLConnectionString"));
        });

        return services;
    }
}