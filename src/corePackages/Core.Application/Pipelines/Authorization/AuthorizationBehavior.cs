using Core.Application.CQRS;
using Core.CrossCuttingConcerns.Exceptions;
using Core.Security.Entities;
using Microsoft.AspNetCore.Http;
using Core.Security.Extensions;

namespace Core.Application.Pipelines.Authorization;

public class AuthorizationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>, ISecuredRequest
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AuthorizationBehavior(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (request.Roles == null || request.Roles.Length == 0)
        {
            throw new InvalidOperationException($"{request.GetType().Name} must define at least one role in {nameof(ISecuredRequest)}.{nameof(ISecuredRequest.Roles)}.");
        }

        List<string>? roleClaims = _httpContextAccessor.HttpContext.User.ClaimRoles();

        if (roleClaims == null) throw new AuthorizationException("Claims not found.");

        bool isNotMatchedARoleClaimWithRequestRoles =
            string.IsNullOrEmpty(roleClaims.FirstOrDefault(roleClaim => request.Roles.Any(role => role == roleClaim)));

        if (isNotMatchedARoleClaimWithRequestRoles) throw new AuthorizationException("You are not authorized.");

        TResponse response = await next();
        return response;
    }
}