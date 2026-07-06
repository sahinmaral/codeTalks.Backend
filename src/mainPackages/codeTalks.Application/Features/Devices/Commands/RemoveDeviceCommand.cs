using codeTalks.Application.Features.Auths.Rules;
using codeTalks.Application.Services;
using codeTalks.Application.Services.Repositories;
using Core.Application.CQRS;

namespace codeTalks.Application.Features.Devices.Commands;

public class RemoveDeviceCommand : ICommand
{
    public string DeviceToken { get; set; }
    
    public class RemoveDeviceCommandHandler(
        IUserDeviceRepository repository,
        ICurrentUserService currentUserService,
        AuthBusinessRules authBusinessRules
        )
        : ICommandHandler<RemoveDeviceCommand>
    {
        public async Task<Unit> Handle(RemoveDeviceCommand cmd, CancellationToken ct)
        {
            var currentUserId = await currentUserService.GetCurrentUserIdAsync();

            await authBusinessRules.CheckUserExistsById(currentUserId);
            
            var device = await repository.GetAsync(
                x => x.UserId == currentUserId && x.DeviceToken == cmd.DeviceToken, ct);

            if (device is null)
                return Unit.Value;

            await repository.DeleteAsync(device, ct);
            
            return Unit.Value;
        }
    }
}