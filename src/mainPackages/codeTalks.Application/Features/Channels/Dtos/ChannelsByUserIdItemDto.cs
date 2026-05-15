using codeTalks.Domain;

namespace codeTalks.Application.Features.Channels.Dtos;

public class ChannelsByUserIdItemDto
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public ChannelUserStatus Status { get; set; }
    public ChannelsByUserIdRoleDto Role { get; set; }
}

public class ChannelsByUserIdRoleDto
{
    public string Id { get; set; }
    public string Name { get; set; }
}