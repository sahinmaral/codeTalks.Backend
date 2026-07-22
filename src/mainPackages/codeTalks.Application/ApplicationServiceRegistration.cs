using codeTalks.Application.Features.Auths.Rules;
using codeTalks.Application.Features.Channels.Rules;
using codeTalks.Application.Services;
using codeTalks.Application.Services.Notifications;
using Core.Application.CQRS;
using Core.Application.Pipelines.Validation;
using FluentValidation;
using Mapster;
using MapsterMapper;
using Microsoft.Extensions.DependencyInjection;

namespace codeTalks.Application;

public static class ApplicationServiceRegistration
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        var assembly = AssemblyReference.Assembly;
        services.AddRequestHandlers(assembly);

        services.AddTransient<IDispatcher, Dispatcher>();

        TypeAdapterConfig.GlobalSettings.Scan(assembly);
        services.AddMapster();

        services.AddValidatorsFromAssembly(assembly);
        
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(RequestValidationBehavior<,>));

        services.AddScoped<AuthBusinessRules>();
        services.AddScoped<ChannelBusinessRules>();
        
        services.AddSingleton<NotificationDecisionEngine>();

        return services;
    }
}