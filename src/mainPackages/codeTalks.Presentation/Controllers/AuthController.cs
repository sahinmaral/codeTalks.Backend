using codeTalks.Application.Features.Auths.Commands.LoginUser;
using codeTalks.Application.Features.Auths.Commands.RefreshToken;
using codeTalks.Application.Features.Auths.Commands.RegisterUser;
using codeTalks.Application.Features.Auths.Dtos;
using codeTalks.Presentation.Controllers.Common;
using Microsoft.AspNetCore.Mvc;

namespace codeTalks.Presentation.Controllers;

public class AuthController : BaseController
{
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterUserCommand request)
    {
        RegisteredUserDto response = await mediator.Send(request);
        return Created("", response);
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginUserCommand request)
    {
        LoggedUserDto response = await mediator.Send(request);
        return Ok(response);
    }
    
    [HttpPost("refresh-token")]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenCommand request)
    {
        RefreshedTokenDto response = await mediator.Send(request);
        return Ok(response);
    }
}