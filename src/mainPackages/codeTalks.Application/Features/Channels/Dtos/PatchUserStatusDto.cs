using codeTalks.Domain;

namespace codeTalks.Application.Features.Channels.Dtos;

public class PatchUserStatusDto
{
    public ChannelUserStatus Status { get; set; }
}