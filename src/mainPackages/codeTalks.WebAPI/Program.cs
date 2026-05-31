using codeTalks.Persistance;
using codeTalks.Application;
using codeTalks.Infrastructure;
using codeTalks.Persistance.Contexts;
using codeTalks.Presentation;
using codeTalks.Presentation.Hubs;
using Core.CrossCuttingConcerns.Exceptions;
using Core.Security;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddControllers(options => options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true)
    .AddApplicationPart(codeTalks.Application.AssemblyReference.Assembly);

builder.Services.AddHttpContextAccessor();

builder.Services
    .AddPersistanceServices(builder.Configuration)
    .AddApplicationServices()
    .AddInfrastructureService()
    .AddPresentationServices();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


builder.Services.AddCors(options => 
    options.AddPolicy("CorsPolicy", policyBuilder =>
    {
        policyBuilder.AllowAnyHeader()
            .AllowAnyMethod()
            .SetIsOriginAllowed(_ =>true)
            .AllowCredentials();
    }));

builder.Services.AddSecurityServices<AppDbContext>();

builder.Services.Configure<RouteOptions>(options =>
{
    options.LowercaseUrls = true;
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

app.UseCors("CorsPolicy");
app.MapHub<ChatHub>("/chatHub");

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(opt => { opt.DisplayRequestDuration(); opt.SwaggerEndpoint("/swagger/v1/swagger.json", "codeTalks"); });
}

app.ConfigureCustomExceptionMiddleware();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

