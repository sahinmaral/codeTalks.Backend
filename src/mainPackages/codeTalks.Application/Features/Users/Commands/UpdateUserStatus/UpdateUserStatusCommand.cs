using System.Security.Claims;
using codeTalks.Application.Features.Users.Helpers;
using codeTalks.Application.Services.Repositories;
using codeTalks.Domain;
using Core.Application.CQRS;
using Core.Security.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;

namespace codeTalks.Application.Features.Users.Commands.UpdateUserStatus;

public class UpdateUserStatusCommand : ICommand
{
    public UserStatusType Status { get; set; }

    public class UpdateUserStatusCommandHandler(
        IHttpContextAccessor httpContextAccessor,
        UserManager<User> userManager,
        IUserStatusRepository userStatusRepository) : ICommandHandler<UpdateUserStatusCommand>
    {
        public async Task<Unit> Handle(UpdateUserStatusCommand request, CancellationToken cancellationToken)
        {
            var currentUserId = await UserContextHelper.GetCurrentUserId(httpContextAccessor, userManager);
            
            var currentUserStatus = await userStatusRepository.GetAsync(x => x.UserId == currentUserId);
            if (currentUserStatus == null)
            {
                await userStatusRepository.AddAsync(new UserStatus
                {
                    Status = request.Status,
                    UserId = currentUserId
                });
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