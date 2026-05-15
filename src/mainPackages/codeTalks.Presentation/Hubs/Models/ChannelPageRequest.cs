using Newtonsoft.Json;

namespace codeTalks.Presentation.Hubs.Models;

public class ChannelPageRequest
{
    public required string UserId { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}