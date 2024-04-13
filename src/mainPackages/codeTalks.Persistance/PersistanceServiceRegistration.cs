using codeTalks.Application.Services.Repositories;
using codeTalks.Persistance.Contexts;
using codeTalks.Persistance.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace codeTalks.Persistance;

public static class PersistanceServiceRegistration
{
    public static IServiceCollection AddPersistanceServices(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("MSSQLConnectionString");

        services.AddScoped<IMessageRepository, MessageRepository>();
        services.AddScoped<IChannelRepository, ChannelRepository>();

        services.AddDbContext<AppDbContext>(options =>
        {
            options.UseSqlServer(connectionString);
        });

        return services;
    }
}