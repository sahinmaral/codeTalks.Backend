namespace codeTalks.Application.Features.Auths.Dtos;

public class RegisteredUserDto
{
    public string Id { get; init; }
    public string FirstName { get; init; }
    public string? MiddleName { get; init; }
    public string LastName { get; init; }
    public string? ProfilePhotoURL { get; init; }
    public string UserName { get; init; }
    public string Email { get; init; }
}
