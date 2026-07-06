using Core.Persistence.Repositories;
using Core.Security.Entities;

namespace codeTalks.Domain;

public class UserDevice : Entity
{
    public string UserId { get; set; }
    public User User { get; set; }
    public string DeviceToken { get; set; }
}