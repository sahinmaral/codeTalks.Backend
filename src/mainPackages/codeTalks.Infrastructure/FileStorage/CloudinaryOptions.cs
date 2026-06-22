namespace codeTalks.Infrastructure.FileStorage;

public sealed class CloudinaryOptions
{
    public string CloudName { get; set; } = null!;
    public string APIKey { get; set; } = null!;
    public string APISecret { get; set; } = null!;
}