using codeTalks.Persistence;
using codeTalks.Application;
using codeTalks.Infrastructure;
using codeTalks.Infrastructure.Hubs;
using codeTalks.Persistence.Contexts;
using codeTalks.Presentation;
using codeTalks.Presentation.Hubs;
using codeTalks.WebAPI.HealthChecks;
using Core.CrossCuttingConcerns.Exceptions;
using Core.Security;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Serilog.Formatting.Compact;
using System.Globalization;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, services, loggerConfig) =>
{
    loggerConfig
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .Enrich.WithEnvironmentName();

    if (context.HostingEnvironment.IsDevelopment())
        loggerConfig.WriteTo.Console();
    else
        loggerConfig.WriteTo.Console(new CompactJsonFormatter());
});

builder.Services
    .AddControllers(options => options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true)
    .AddApplicationPart(codeTalks.Application.AssemblyReference.Assembly);

builder.Services.AddHttpContextAccessor();

builder.Services
    .AddPersistanceServices(builder.Configuration)
    .AddApplicationServices()
    .AddInfrastructureService(builder.Configuration)
    .AddPresentationServices();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");

builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    CultureInfo[] supportedCultures = [new("en"), new("tr")];

    options.DefaultRequestCulture = new RequestCulture("en");
    options.SupportedCultures = supportedCultures;
    options.SupportedUICultures = supportedCultures;
    options.RequestCultureProviders = [new AcceptLanguageHeaderRequestCultureProvider()];
});

builder.Services.AddSecurityServices<AppDbContext>();

builder.Services.Configure<RouteOptions>(options =>
{
    options.LowercaseUrls = true;
});

builder.Services.AddHealthChecks()
    .AddCheck<PostgresHealthCheck>("postgres", tags: ["ready"])
    .AddCheck<RedisHealthCheck>("redis", tags: ["ready"])
    .AddCheck<RabbitMqHealthCheck>("rabbitmq", tags: ["ready"]);

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var dbContext = services.GetRequiredService<AppDbContext>();
    try
    {
        dbContext.Database.Migrate();
    }
    catch (Exception ex)
    {
        throw new Exception("Error applying database migrations.", ex);
    }
}

app.UseRequestLocalization();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(opt => { opt.DisplayRequestDuration(); opt.SwaggerEndpoint("/swagger/v1/swagger.json", "codeTalks"); });
}

app.ConfigureCustomExceptionMiddleware();

app.UseSerilogRequestLogging();

app.UseAuthentication();
app.UseAuthorization();

app.MapHub<ChatHub>("/hubs/chat");
app.MapHub<NotificationHub>("/hubs/notifications");
app.MapControllers();

app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false });
app.MapHealthChecks("/health/ready", new HealthCheckOptions { Predicate = check => check.Tags.Contains("ready") });

app.Run();

public partial class Program;

