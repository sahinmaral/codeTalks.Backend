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
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Serilog.Formatting.Compact;
using System.Globalization;
using System.Threading.RateLimiting;

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

// Dsn comes from configuration (Sentry:Dsn) -- User Secrets locally, an environment
// variable in real deployments -- never hardcoded here. Sentry requires an explicit
// empty string to disable itself; a merely-absent config key makes it throw at
// startup instead, hence the ?? string.Empty fallback for environments (CI, a fresh
// clone) with no Sentry account configured.
builder.WebHost.UseSentry(options =>
{
    options.Dsn = builder.Configuration["Sentry:Dsn"] ?? string.Empty;
    options.Environment = builder.Environment.EnvironmentName;
    options.TracesSampleRate = 0.0; // error tracking only, no performance/tracing product
    options.Debug = builder.Environment.IsDevelopment();
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

// Limits are configuration-driven (not hardcoded) so the integration test harness --
// which drives ~100 auth calls through one shared host from a single loopback address
// in seconds -- can override them to stay out of the way, while real deployments keep
// the strict defaults below.
var globalRateLimit = builder.Configuration.GetSection("RateLimiting:Global");
var authRateLimit = builder.Configuration.GetSection("RateLimiting:Auth");

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = globalRateLimit.GetValue("PermitLimit", 100),
                Window = TimeSpan.FromSeconds(globalRateLimit.GetValue("WindowSeconds", 60))
            }));

    // Stricter policy for auth endpoints (register/login/refresh) -- prime targets for
    // credential stuffing / brute force / fake-account creation.
    options.AddPolicy("auth", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = authRateLimit.GetValue("PermitLimit", 5),
                Window = TimeSpan.FromSeconds(authRateLimit.GetValue("WindowSeconds", 60))
            }));

    options.OnRejected = async (context, cancellationToken) =>
    {
        context.HttpContext.Response.ContentType = "application/json";
        await context.HttpContext.Response.WriteAsync(new RateLimitProblemDetails
        {
            Status = StatusCodes.Status429TooManyRequests,
            Type = "https://example.com/probs/rateLimit",
            Title = "Too many requests",
            Detail = "Rate limit exceeded. Please try again later.",
            Instance = ""
        }.ToString(), cancellationToken);
    };
});

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

app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

app.MapHub<ChatHub>("/hubs/chat");
app.MapHub<NotificationHub>("/hubs/notifications");
app.MapControllers();

// Exempt from rate limiting: orchestrators/load balancers poll these frequently, and
// getting rate-limited would be indistinguishable from a genuine outage.
app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false }).DisableRateLimiting();
app.MapHealthChecks("/health/ready", new HealthCheckOptions { Predicate = check => check.Tags.Contains("ready") }).DisableRateLimiting();

app.Run();

public partial class Program;

