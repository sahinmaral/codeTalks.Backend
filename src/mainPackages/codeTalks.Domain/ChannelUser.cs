using Core.Security.Entities;

namespace codeTalks.Domain;

public class ChannelUser
{
    public string ChannelId { get; set; }
    public Channel Channel { get; set; }
    
    public string UserId { get; set; }
    public User User { get; set; }

    public ChannelUserStatus Status { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public Role Role { get; set; }
    public string RoleId { get; set; }
}