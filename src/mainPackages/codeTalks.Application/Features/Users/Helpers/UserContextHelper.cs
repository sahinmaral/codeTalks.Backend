using System.Security.Claims;
using Core.Security.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;

namespace codeTalks.Application.Features.Users.Helpers;

public static class UserContextHelper
{
    public static async Task<string> GetCurrentUserId(IHttpContextAccessor httpContextAccessor, UserManager<User> userManager)
    {
        try
        {
            var httpContext = httpContextAccessor.HttpContext;
            if (httpContext == null)
            {
                // Handle the case where HttpContext is null
                throw new InvalidOperationException("HttpContext is null");
            }

            var userIdClaim = httpContext.User?.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
            {
                // Handle the case where the user ID claim is not found
                throw new InvalidOperationException("User ID claim not found");
            }
            
            var userId =  userIdClaim.Value;
            
            var user = await userManager.FindByIdAsync(userId);
            if (user == null)
            {
                throw new InvalidOperationException("User not found");
            }

            return userId;
        }
        catch (Exception ex)
        {
            // Log the exception or handle it as needed
            throw new SecurityTokenException($"Error getting current user ID: {ex.Message}");
        }
    }
}