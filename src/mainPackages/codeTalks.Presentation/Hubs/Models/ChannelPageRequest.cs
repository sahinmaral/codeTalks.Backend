using Newtonsoft.Json;

namespace codeTalks.Presentation.Hubs.Models;

public class ChannelPageRequest
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public string? Title { get; set; }
}