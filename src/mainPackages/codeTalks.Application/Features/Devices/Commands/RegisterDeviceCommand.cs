using codeTalks.Application.Features.Auths.Rules;
using codeTalks.Application.Services;
using codeTalks.Application.Services.Repositories;
using codeTalks.Domain;
using Core.Application.CQRS;
using Core.Persistence.Repositories;

namespace codeTalks.Application.Features.Devices.Commands;

public record RegisterDeviceCommand : ICommand
{
    public string DeviceToken { get; set; }
    
    public class RegisterDeviceCommandHandler(
        IUserDeviceRepository repository,  
        ICurrentUserService currentUserService,
        AuthBusinessRules authBusinessRules
        )
        : ICommandHandler<RegisterDeviceCommand>
    {
        public async Task<Unit> Handle(RegisterDeviceCommand cmd, CancellationToken ct)
        {
            var currentUserId = await currentUserService.GetCurrentUserIdAsync();
            
            await authBusinessRules.CheckUserExistsById(currentUserId);
            
            var existing = await repository.GetAsync(
                x => x.UserId == currentUserId && x.DeviceToken == cmd.DeviceToken, ct);

            if (existing is not null)
                return Unit.Value;

            var device = new UserDevice
            {
                UserId      = currentUserId,
                DeviceToken = cmd.DeviceToken,
                CreatedAt   = DateTime.UtcNow
            };

            await repository.AddAsync(device, ct);

            return Unit.Value;
        }
    }
}