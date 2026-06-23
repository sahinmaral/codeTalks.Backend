using codeTalks.Domain;

namespace codeTalks.Application.Features.Channels.Dtos;

public class PatchChannelDto
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public ChannelJoinPolicy? JoinPolicy { get; set; }
}