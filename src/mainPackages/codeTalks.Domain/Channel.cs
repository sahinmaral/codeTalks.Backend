using Core.Persistence.Repositories;

namespace codeTalks.Domain;

public class Channel : Entity
{
    public string Name { get; set; }
    public string Description { get; set; }
    public string InviteCode { get; set; } 
    public DateTime? DeletedAt { get; set; }
    public bool IsActive { get; set; } = true;
    
    public ICollection<Message> Messages { get; set; }
    public ICollection<ChannelUser> ChannelUsers { get; set; }
}