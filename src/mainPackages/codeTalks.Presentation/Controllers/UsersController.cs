using codeTalks.Application.Features.Users.Commands.ChangeUserPassword;
using codeTalks.Application.Features.Users.Commands.UpdateUserStatus;
using codeTalks.Presentation.Controllers.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace codeTalks.Presentation.Controllers;

public class UsersController : BaseController
{

    [Authorize]
    [HttpPut("status")]
    public async Task<IActionResult> UpdateUserStatus([FromBody] UpdateUserStatusCommand request)
    {
        await Dispatcher.SendAsync(request);

        return NoContent();
    }

    [Authorize]
    [HttpPut("password")]
    public async Task<IActionResult> ChangeUserPassword([FromBody] ChangeUserPasswordCommand request)
    {
        await Dispatcher.SendAsync(request);

        return NoContent();
    }
}