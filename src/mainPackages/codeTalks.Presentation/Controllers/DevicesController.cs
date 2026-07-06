using codeTalks.Application.Features.Devices.Commands;
using codeTalks.Presentation.Controllers.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace codeTalks.Presentation.Controllers;

[Authorize]
public class DevicesController : BaseController
{
    [HttpPost("register")]
    public async Task<IActionResult> Register(
        [FromBody] RegisterDeviceCommand request, CancellationToken ct)
    {
        await Dispatcher.SendAsync(request, ct);

        return Ok();
    }

    [HttpDelete("remove")]
    public async Task<IActionResult> Remove(
        [FromBody] RemoveDeviceCommand request, CancellationToken ct)
    {
        await Dispatcher.SendAsync(request, ct);

        return Ok();
    }
}