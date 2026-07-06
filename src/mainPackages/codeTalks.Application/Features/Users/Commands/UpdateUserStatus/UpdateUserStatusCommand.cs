using codeTalks.Application.Features.Channels.Dtos;
using codeTalks.Application.Services;
using codeTalks.Application.Services.Repositories;
using codeTalks.Domain;
using Core.Application.CQRS;

namespace codeTalks.Application.Features.Users.Commands.UpdateUserStatus;

public class UpdateUserStatusCommand : ICommand
{
    public UserStatusType Status { get; set; }

    public class UpdateUserStatusCommandHandler(
        ICurrentUserService currentUserService,
        IUserStatusRepository userStatusRepository) : ICommandHandler<UpdateUserStatusCommand>
    {
        public async Task<Unit> Handle(UpdateUserStatusCommand request,
            CancellationToken cancellationToken)
        {
            var currentUserId = await currentUserService.GetCurrentUserIdAsync();
            
            var currentUserStatus = await userStatusRepository.GetAsync(x => x.UserId == currentUserId);
            if (currentUserStatus == null)
            {
                await userStatusRepository.AddAsync(new UserStatus
                {
                    Status = request.Status,
                    UserId = currentUserId
                }, cancellationToken);
            }
            else
            {
                currentUserStatus.Status = request.Status;
                userStatusRepository.Update(currentUserStatus);
            }
            
            return Unit.Value;
        }
    }
}