using System.Security.Claims;
using codeTalks.Application.Services;
using Core.CrossCuttingConcerns.Exceptions;
using Core.Security.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;

namespace codeTalks.Infrastructure.Services;

public class CurrentUserService(
    IHttpContextAccessor httpContextAccessor,
    UserManager<User> userManager) : ICurrentUserService
{
    public async Task<string> GetCurrentUserIdAsync()
    {
        var httpContext = httpContextAccessor.HttpContext
            ?? throw new InvalidOperationException("HttpContext is null");

        var userIdClaim = httpContext.User?.FindFirst(ClaimTypes.NameIdentifier)
            ?? throw new AuthorizationException("User identity could not be resolved");

        var user = await userManager.FindByIdAsync(userIdClaim.Value)
            ?? throw new AuthorizationException("User no longer exists");

        return userIdClaim.Value;
    }
}