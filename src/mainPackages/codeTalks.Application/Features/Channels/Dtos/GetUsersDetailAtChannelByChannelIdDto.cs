namespace codeTalks.Application.Features.Channels.Dtos;

public class GetUsersDetailAtChannelByChannelIdDto
{
    public string Id { get; set; }
    public string FirstName { get; set; }
    public string? MiddleName { get; set; }
    public string LastName { get; set; } = null!;
    public string? ProfilePhotoURL { get; set; }
    public string UserName { get; set; }
    public string Email { get; set; }
    public UserRoleAtChannelDto Role { get; set; }
}

public class UserRoleAtChannelDto
{
    public string Id { get; set; }
    public string Name { get; set; }
}