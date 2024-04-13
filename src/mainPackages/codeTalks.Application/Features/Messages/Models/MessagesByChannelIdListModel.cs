using codeTalks.Application.Features.Messages.Dtos;
using Core.Persistence.Paging;

namespace codeTalks.Application.Features.Messages.Models;

public class MessagesByChannelIdListModel : BasePageableModel
{
    public IList<MessagesByChannelIdDto> Items { get; set; }
}