using Core.Persistence.Repositories;
using Core.Security.Entities;

namespace codeTalks.Domain;

public class UserStatus : Entity
{
    public string UserId { get; set; }
    public User User { get; set; }
    public UserStatusType Status { get; set; }
}