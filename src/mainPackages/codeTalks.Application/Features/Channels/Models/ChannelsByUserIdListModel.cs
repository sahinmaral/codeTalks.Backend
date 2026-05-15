using codeTalks.Application.Features.Channels.Dtos;
using Core.Persistence.Paging;

namespace codeTalks.Application.Features.Channels.Models;

public class ChannelsByUserIdListModel : BasePageableModel
{
    public IList<ChannelsByUserIdItemDto> Items { get; set; }
}