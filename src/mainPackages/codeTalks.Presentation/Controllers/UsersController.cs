using codeTalks.Application.Features.Users.Commands.ChangeUserPassword;
using codeTalks.Application.Features.Users.Commands.DeleteProfilePhoto;
using codeTalks.Application.Features.Users.Commands.UpdateProfilePhoto;
using codeTalks.Application.Features.Users.Commands.UpdateUserStatus;
using codeTalks.Application.Features.Users.Dtos;
using codeTalks.Presentation.Controllers.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
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
    
    [HttpPut("profile-photo")]
    public async Task<IActionResult> UpdateProfilePhoto(IFormFile image)
    {
        UpdateProfilePhotoCommand request = new UpdateProfilePhotoCommand()
        {
            Image = image,
        };
        
        UpdatedProfilePhotoDto response = await Dispatcher.SendAsync(request);
        
        return Ok(response);
    }
    
    [HttpDelete("profile-photo")]
    public async Task<IActionResult> DeleteProfilePhoto()
    {
        DeleteProfilePhotoCommand request = new DeleteProfilePhotoCommand();
        await Dispatcher.SendAsync(request);
        
        return NoContent();
    }
}