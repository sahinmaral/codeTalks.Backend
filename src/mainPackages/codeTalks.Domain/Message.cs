using Core.Persistence.Repositories;
using Core.Security.Entities;

namespace codeTalks.Domain;

public class Message : Entity
{
    public string Content { get; set; }
    
    public User Sender { get; set; }
    public string SenderId { get; set; }

    public Channel Channel { get; set; }
    public string ChannelId { get; set; }
}