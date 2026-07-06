using Core.Persistence.Repositories;
using Core.Security.Entities;

namespace codeTalks.Domain;

public class UserNotificationSetting : Entity
{
    public User User { get; set; }
    public string UserId { get; set; }
    public bool IsEnabled { get; set; }
    public bool IsSoundEnabled { get; set; }
    
    public void Update(bool isEnabled, bool isSoundEnabled)
    {
        IsEnabled = isEnabled;
        IsSoundEnabled = isSoundEnabled;
    }
}