using Core.Application.CQRS;
using Core.CrossCuttingConcerns.Logging;
using Core.CrossCuttingConcerns.Logging.Serilog;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;

namespace Core.Application.Pipelines.Logging;

public class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>, ILoggableRequest
{
    private readonly LoggerServiceBase _loggerServiceBase;
    private readonly IHttpContextAccessor _httpContextAccessor;


    public LoggingBehavior(LoggerServiceBase loggerServiceBase, IHttpContextAccessor httpContextAccessor)
    {
        _loggerServiceBase = loggerServiceBase;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        string user = _httpContextAccessor.HttpContext?.User.Identity?.Name ?? "?";

        List<LogParameter> logParameters = new()
        {
            new LogParameter { Type = request.GetType().Name, Value = request }
        };

        LogDetail logDetail = new()
        {
            MethodName = next.Method.Name,
            Parameters = logParameters,
            User = user
        };

        _loggerServiceBase.Info(JsonConvert.SerializeObject(logDetail));

        try
        {
            TResponse response = await next();
            _loggerServiceBase.Info(JsonConvert.SerializeObject(new
            {
                logDetail.MethodName,
                logDetail.User,
                Response = response
            }));
            return response;
        }
        catch (Exception ex)
        {
            LogDetailWithException errorLog = new()
            {
                MethodName = logDetail.MethodName,
                Parameters = logParameters,
                User = user,
                ExceptionMessage = ex.Message
            };
            _loggerServiceBase.Error(JsonConvert.SerializeObject(errorLog));
            throw;
        }
    }
}