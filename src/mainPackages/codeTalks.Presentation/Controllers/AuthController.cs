using codeTalks.Application.Features.Auths.Commands.LoginUser;
using codeTalks.Application.Features.Auths.Commands.RefreshToken;
using codeTalks.Application.Features.Auths.Commands.RegisterUser;
using codeTalks.Application.Features.Auths.Dtos;
using codeTalks.Application.Features.Auths.Queries.GetCurrentUser;
using codeTalks.Presentation.Controllers.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace codeTalks.Presentation.Controllers;

public class AuthController : BaseController
{
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterUserCommand request)
    {
        RegisteredUserDto response = await Dispatcher.SendAsync(request);
        return Created("", response);
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginUserCommand request)
    {
        LoggedUserDto response = await Dispatcher.SendAsync(request);
        return Ok(response);
    }
    
    [Authorize]
    [HttpPost("refresh-token")]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenCommand request)
    {
        RefreshedTokenDto response = await Dispatcher.SendAsync(request);
        return Ok(response);
    }
    
    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> GetCurrentUser()
    {
        GetCurrentUserDto response = await Dispatcher.SendAsync(new GetCurrentUserQuery());
        return Ok(response);
    }
}