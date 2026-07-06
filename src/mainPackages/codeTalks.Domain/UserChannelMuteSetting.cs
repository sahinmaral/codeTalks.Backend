using Core.Persistence.Repositories;
using Core.Security.Entities;

namespace codeTalks.Domain;

public class UserChannelMuteSetting : Entity
{
    public User User { get; set; }
    public string UserId { get; set; }
    
    public Channel Channel { get; set; }
    public string ChannelId { get; set; }
    public DateTime? MutedUntil { get; set; } 

    public bool IsMuted => MutedUntil == null || MutedUntil > DateTime.UtcNow;

    public void Mute(DateTime? until = null) => MutedUntil = until;
    public void Unmute() => MutedUntil = DateTime.MinValue;
}