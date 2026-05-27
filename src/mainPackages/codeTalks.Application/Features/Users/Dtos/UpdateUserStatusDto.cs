using codeTalks.Domain;

namespace codeTalks.Application.Features.Users.Dtos;

public class UpdateUserStatusDto
{
    public UserStatusType Status { get; set; }
}