using codeTalks.Domain;

namespace codeTalks.Application.Features.Auths.Dtos;

public class GetUserStatusDto
{
    public UserStatusType Status { get; init; }
    public DateTime LastUpdated { get; init; }
}