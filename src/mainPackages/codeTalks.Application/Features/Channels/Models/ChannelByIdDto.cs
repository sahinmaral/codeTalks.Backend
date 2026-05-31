using codeTalks.Application.Features.Channels.Dtos;

namespace codeTalks.Application.Features.Channels.Models;

public class ChannelByIdDto
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public string InviteCode { get; set; }
    public DateTime CreatedAt { get; set; }
    public int MemberCount { get; set; }
    public ChannelsByUserIdRoleDto Role { get; set; }
}