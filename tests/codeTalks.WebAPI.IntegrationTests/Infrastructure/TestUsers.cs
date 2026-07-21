using codeTalks.Application.Features.Auths.Commands.RegisterUser;

namespace codeTalks.WebAPI.IntegrationTests.Infrastructure;

/// <summary>
/// Builds registration payloads with unique usernames/emails. The whole assembly shares
/// one Postgres container, so every test must register a distinct user to avoid tripping
/// the unique-username / unique-email rules.
/// </summary>
public static class TestUsers
{
    public const string DefaultPassword = "Passw0rd";

    public static RegisterUserCommand New(string password = DefaultPassword)
    {
        // "N" format is hex (0-9a-f) — all within Identity's AllowedUserNameCharacters.
        var token = Guid.NewGuid().ToString("N");
        return new RegisterUserCommand
        {
            FirstName = "Test",
            LastName = "User",
            UserName = $"u{token}",
            Email = $"{token}@test.local",
            Password = password
        };
    }
}