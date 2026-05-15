using codeTalks.Application.Features.Users.Dtos;
using Core.Persistence.Paging;

namespace codeTalks.Application.Features.Users.Models;

public class UsersByChannelIdListModel : BasePageableModel
{
    public IList<UsersByChannelIdDto> Items { get; set; }
}