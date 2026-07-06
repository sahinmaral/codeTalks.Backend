namespace codeTalks.Application.Features.Users.Dtos;

public class UserChannelMuteSettingDto
{
    public string ChannelId { get; set; }
    public DateTime? MutedUntil { get; set; } 
    public bool IsMuted => MutedUntil == null || MutedUntil > DateTime.UtcNow;
}