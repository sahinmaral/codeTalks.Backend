namespace codeTalks.Application.Features.Users.Dtos;

public class GetCurrentUserDto
{
    public string Id { get; init; }
    public string FirstName { get; init; }
    public string? MiddleName { get; init; }
    public string LastName { get; init; }
    public string? ProfilePhotoURL { get; init; }
    public string UserName { get; init; }
    public string Email { get; init; }
    
    public string? Bio { get; init; }
    public int JoinedChannelCount { get; set; }
    public DateTime CreatedAt { get; init; }

    public GetUserStatusDto UserStatus { get; set; }
}