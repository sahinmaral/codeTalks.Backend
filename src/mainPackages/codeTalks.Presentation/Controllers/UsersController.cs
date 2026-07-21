using codeTalks.Application.Features.Users.Commands.ChangeUserPassword;
using codeTalks.Application.Features.Users.Commands.DeleteProfilePhoto;
using codeTalks.Application.Features.Users.Commands.UnmuteChannel;
using codeTalks.Application.Features.Users.Commands.UpdateChannelMuteSetting;
using codeTalks.Application.Features.Users.Commands.UpdateProfileInformation;
using codeTalks.Application.Features.Users.Commands.UpdateUserNotificationSetting;
using codeTalks.Application.Features.Users.Commands.UpdateProfilePhoto;
using codeTalks.Application.Features.Users.Commands.UpdateUserStatus;
using codeTalks.Application.Features.Users.Dtos;
using codeTalks.Application.Features.Users.Queries.GetCurrentUser;
using codeTalks.Application.Features.Users.Queries.GetUserChannelMuteSettings;
using codeTalks.Application.Features.Users.Queries.GetUserNotificationSettings;
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
    
    [Authorize]
    [HttpPut("profile")]
    public async Task<IActionResult> UpdateProfileInformations([FromBody] UpdateProfileInformationDto request)
    {
        UpdateProfileInformationCommand command = new UpdateProfileInformationCommand()
        {
            ProfileInformation = request,
        };
        
        await Dispatcher.SendAsync(command);
        
        return Ok();
    }
    
    [Authorize]
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
    
    [Authorize]
    [HttpDelete("profile-photo")]
    public async Task<IActionResult> DeleteProfilePhoto()
    {
        DeleteProfilePhotoCommand request = new DeleteProfilePhotoCommand();
        await Dispatcher.SendAsync(request);
        
        return NoContent();
    }
    
    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> GetCurrentUser()
    {
        GetCurrentUserDto response = await Dispatcher.SendAsync(new GetCurrentUserQuery());
        return Ok(response);
    }
    
    [Authorize]
    [HttpGet("me/notification-settings")]
    public async Task<IActionResult> GetCurrentUserNotificationSettings()
    {
        UserNotificationSettingDto response = await Dispatcher.SendAsync(new GetUserNotificationSettingsQuery());
        return Ok(response);
    }
    
    [Authorize]
    [HttpGet("me/channel-mute-settings")]
    public async Task<IActionResult> GetCurrentUserChannelMuteSettings()
    {
        List<UserChannelMuteSettingDto> response = await Dispatcher.SendAsync(new GetUserChannelMuteSettingsQuery());
        return Ok(response);
    }
    
    [Authorize]
    [HttpPut("me/channel-mute-settings/{channelId}")]
    public async Task<IActionResult> UpdateChannelMuteSetting([FromRoute]string channelId, [FromBody]UpdateChannelMuteSettingDto request)
    {
        await Dispatcher.SendAsync(new UpdateChannelMuteSettingCommand
        {
            ChannelId = channelId,
            MuteUntil = request.MuteUntil
        });
        return Ok();
    }

    [Authorize]
    [HttpDelete("me/channel-mute-settings/{channelId}")]
    public async Task<IActionResult> UnmuteChannel([FromRoute]string channelId)
    {
        await Dispatcher.SendAsync(new UnmuteChannelCommand
        {
            ChannelId = channelId
        });
        return NoContent();
    }
    
        
    [Authorize]
    [HttpPut("me/notification-settings")]
    public async Task<IActionResult> UpdateUserNotificationSetting([FromBody]UpdateUserNotificationSettingCommand request)
    {
        await Dispatcher.SendAsync(request);
        return NoContent();
    }
}