using codeTalks.Application.Services;
using codeTalks.Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;

namespace codeTalks.Infrastructure;

public static class InfrastructureServiceRegistration
{
    public static IServiceCollection AddInfrastructureService(this IServiceCollection services)
    {
        services.AddScoped<IInviteCodeGenerator, InviteCodeGenerator>();
        services.AddScoped<ICurrentUserService, CurrentUserService>();

        return services;
    }
}