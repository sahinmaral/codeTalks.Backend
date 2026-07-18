namespace codeTalks.Application.Features.Users.Dtos;

public class UpdateProfileInformationDto
{
    public string FirstName { get; set; }
    public string? MiddleName { get; set; }
    public string LastName { get; set; }
    public string? Bio { get; set; }
}