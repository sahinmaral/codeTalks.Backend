using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Sentry;
using System.Net;

namespace Core.CrossCuttingConcerns.Exceptions;

public class ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
{
    public async Task Invoke(HttpContext context, IStringLocalizer<SharedResource> localizer)
    {
        try
        {
            await next(context);
        }
        catch (Exception exception)
        {
            await HandleExceptionAsync(context, exception, localizer);
        }
    }

    private Task HandleExceptionAsync(HttpContext context, Exception exception, IStringLocalizer localizer)
    {
        context.Response.ContentType = "application/json";

        if (exception.GetType() == typeof(ValidationException)) return CreateValidationException(context, exception, localizer);
        if (exception.GetType() == typeof(BusinessException)) return CreateBusinessException(context, exception, localizer);
        if (exception.GetType() == typeof(EntityNotFoundException)) return CreateEntityNotFoundException(context, exception, localizer);
        if (exception.GetType() == typeof(AuthorizationException)) return CreateAuthorizationException(context, exception, localizer);
        if (exception.GetType() == typeof(SecurityTokenException)) return CreateSecurityTokenException(context, exception, localizer);
        return CreateInternalException(context, exception, localizer);
    }

    private Task CreateSecurityTokenException(HttpContext context, Exception exception, IStringLocalizer localizer)
    {
        context.Response.StatusCode = Convert.ToInt32(HttpStatusCode.Unauthorized);

        return context.Response.WriteAsync(new SecurityTokenProblemDetails
        {
            Status = StatusCodes.Status401Unauthorized,
            Type = "urn:codetalks:problem:securityToken",
            Title = "Security token exception",
            Detail = localizer[exception.Message],
            Instance = ""
        }.ToString());
    }

    private Task CreateAuthorizationException(HttpContext context, Exception exception, IStringLocalizer localizer)
    {
        context.Response.StatusCode = Convert.ToInt32(HttpStatusCode.Forbidden);

        return context.Response.WriteAsync(new AuthorizationProblemDetails
        {
            Status = StatusCodes.Status403Forbidden,
            Type = "urn:codetalks:problem:authorization",
            Title = "Authorization exception",
            Detail = localizer[exception.Message],
            Instance = ""
        }.ToString());
    }

    private Task CreateEntityNotFoundException(HttpContext context, Exception exception, IStringLocalizer localizer)
    {
        context.Response.StatusCode = Convert.ToInt32(HttpStatusCode.NotFound);

        return context.Response.WriteAsync(new EntityNotFoundProblemDetails
        {
            Status = StatusCodes.Status404NotFound,
            Type = "urn:codetalks:problem:notFound",
            Title = "Entity not found exception",
            Detail = localizer[exception.Message],
            Instance = ""
        }.ToString());
    }

    private Task CreateBusinessException(HttpContext context, Exception exception, IStringLocalizer localizer)
    {
        context.Response.StatusCode = Convert.ToInt32(HttpStatusCode.BadRequest);

        return context.Response.WriteAsync(new BusinessProblemDetails
        {
            Status = StatusCodes.Status400BadRequest,
            Type = "urn:codetalks:problem:business",
            Title = "Business exception",
            Detail = localizer[exception.Message],
            Instance = ""
        }.ToString());
    }

    private Task CreateValidationException(HttpContext context, Exception exception, IStringLocalizer localizer)
    {
        context.Response.StatusCode = Convert.ToInt32(HttpStatusCode.BadRequest);

        List<FluentValidation.Results.ValidationFailure> failures = ((ValidationException)exception).Errors
            .Select(failure =>
            {
                failure.ErrorMessage = localizer[failure.ErrorMessage];
                return failure;
            })
            .ToList();

        return context.Response.WriteAsync(new ValidationProblemDetails
        {
            Status = StatusCodes.Status400BadRequest,
            Type = "urn:codetalks:problem:validation",
            Title = "Validation error(s)",
            Detail = "",
            Instance = "",
            Errors = failures
        }.ToString());
    }

    private Task CreateInternalException(HttpContext context, Exception exception, IStringLocalizer localizer)
    {
        logger.LogError(exception, "Unhandled exception processing {Method} {Path}",
            context.Request.Method, context.Request.Path);
        SentrySdk.CaptureException(exception);

        context.Response.StatusCode = Convert.ToInt32(HttpStatusCode.InternalServerError);

        return context.Response.WriteAsync(new InternalServerErrorProblemDetails
        {
            Status = StatusCodes.Status500InternalServerError,
            Type = "urn:codetalks:problem:internal",
            Title = "Internal exception",
            Detail = localizer[exception.Message],
            Instance = ""
        }.ToString());
    }
}